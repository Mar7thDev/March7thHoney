using March7thHoney.Database;
using March7thHoney.Database.Account;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server;
using March7thHoney.Util;
using March7thHoney.WebServer.Handler;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace March7thHoney.WebServer.Controllers;

public static partial class AdminRoutes
{
    private static void MapAccountRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/api/accounts", GetAccounts);
        app.MapPost("/admin/api/accounts", CreateAccount);
        app.MapPut("/admin/api/accounts/{uid:int}", UpdateAccount);
        app.MapPut("/admin/api/accounts/{uid:int}/password", SetPassword);
        app.MapPost("/admin/api/accounts/{uid:int}/email/verification", SendAccountVerificationEmail);
        app.MapPost("/admin/api/accounts/{uid:int}/ban", BanAccount);
        app.MapDelete("/admin/api/accounts/{uid:int}/ban", UnbanAccount);
        app.MapPost("/admin/api/accounts/{uid:int}/tokens/revoke", RevokeAccountTokens);
        app.MapPost("/admin/api/accounts/{uid:int}/session/kick", KickAccountSession);
        app.MapPost("/admin/api/accounts/{uid:int}/gameplay/reset", ResetAccountGameplay);
        app.MapDelete("/admin/api/accounts/{uid:int}/identity-keys", ClearIdentityKeys);
        app.MapDelete("/admin/api/accounts/{uid:int}", DeleteAccount);
    }

    private static IResult GetAccounts(HttpContext ctx)
    {
        if (!IsAuthorized(ctx, out var unauthorized))
            return unauthorized;

        var accounts = GetAllAccounts()
            .Select(ToAccountSummary)
            .OrderBy(account => account.uid)
            .ToList();

        return Success(new AccountsData(accounts, accounts.Count, GetRoleNames(), NormalizeRole(null)));
    }

    private static IResult CreateAccount(HttpContext ctx, AdminCreateAccountRequest? request)
    {
        if (!IsAuthorized(ctx, out var unauthorized))
            return unauthorized;

        request ??= new AdminCreateAccountRequest();
        var username = (request.username ?? string.Empty).Trim();
        var email = (request.email ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(username))
            return Error("Username cannot be empty");

        if (string.IsNullOrWhiteSpace(email))
            return Error("Email cannot be empty");

        if (string.IsNullOrWhiteSpace(request.password))
            return Error("Password cannot be empty");

        var uid = request.uid.GetValueOrDefault();
        try
        {
            var role = NormalizeRole(request.role);
            AccountHelper.CreateAccount(username, email, request.password, uid, role,
                request.email_verified.GetValueOrDefault());
            var account = AccountData.GetAccountByUserName(username);
            if (account == null)
                return Error("Failed to create account");

            account.SyncPermissionsWithRole();
            account.SetPermissionOverrides(request.permissions);

            return Success(new AccountWrap(ToAccountSummary(account)));
        }
        catch (Exception ex)
        {
            return Error(ex.Message);
        }
    }

    private static async Task<IResult> UpdateAccount(HttpContext ctx, int uid, AdminUpdateAccountRequest? request)
    {
        if (!IsAuthorized(ctx, out var unauthorized))
            return unauthorized;

        request ??= new AdminUpdateAccountRequest();
        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return Error("Account not found", statusCode: StatusCodes.Status404NotFound);

        var username = (request.username ?? string.Empty).Trim();
        var email = (request.email ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(username))
            return Error("Username cannot be empty");

        if (string.IsNullOrWhiteSpace(email))
            return Error("Email cannot be empty");

        var existing = AccountData.GetAccountByUserName(username);
        if (existing != null && existing.Uid != uid)
            return Error("Username already exists");

        var existingEmail = AccountData.GetAccountByEmail(email);
        if (existingEmail != null && existingEmail.Uid != uid)
            return Error("Email already exists");

        var previousNormalizedEmail = account.NormalizedEmail;
        if (string.IsNullOrWhiteSpace(previousNormalizedEmail))
            previousNormalizedEmail = AccountData.NormalizeEmail(account.Email);
        var previousEmailVerified = account.IsEmailVerified;
        var requestedEmailVerified = request.email_verified.GetValueOrDefault(account.IsEmailVerified);

        try
        {
            account.Username = username;
            AccountHelper.SetEmail(account, email, requestedEmailVerified);
            account.SetRole(request.role);
            account.SetPermissionOverrides(request.permissions);
        }
        catch (Exception ex)
        {
            return Error(ex.Message);
        }

        var emailSent = false;
        var emailChanged = !string.Equals(previousNormalizedEmail, account.NormalizedEmail, StringComparison.OrdinalIgnoreCase);
        var changedFromVerifiedToUnverified = previousEmailVerified && !account.IsEmailVerified;
        if (!account.IsEmailVerified && (emailChanged || changedFromVerifiedToUnverified))
            emailSent = await AccountRecoveryHandler.SendVerificationEmailForAccount(account, ctx.Request);

        return Success(new AccountEmailWrap(ToAccountSummary(account), emailSent));
    }

    private static IResult SetPassword(HttpContext ctx, int uid, AdminSetPasswordRequest? request)
    {
        if (!IsAuthorized(ctx, out var unauthorized))
            return unauthorized;

        request ??= new AdminSetPasswordRequest();
        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return Error("Account not found", statusCode: StatusCodes.Status404NotFound);

        try
        {
            AccountHelper.SetPassword(account, request.password ?? string.Empty);
            if (request.revoke_tokens.GetValueOrDefault())
                account.ClearLoginTokens();
            return Success(new AccountWrap(ToAccountSummary(account)));
        }
        catch (Exception ex)
        {
            return Error(ex.Message);
        }
    }

    private static async Task<IResult> SendAccountVerificationEmail(HttpContext ctx, int uid)
    {
        if (!IsAuthorized(ctx, out var unauthorized))
            return unauthorized;

        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return Error("Account not found", statusCode: StatusCodes.Status404NotFound);

        if (string.IsNullOrWhiteSpace(account.Email))
            return Error("Account does not have an email address");

        var emailSent = await AccountRecoveryHandler.SendVerificationEmailForAccount(account, ctx.Request,
            markUnverified: true);
        return Success(new AccountEmailWrap(ToAccountSummary(account), emailSent));
    }

    private static IResult BanAccount(HttpContext ctx, int uid, AdminBanAccountRequest? request)
    {
        if (!IsAuthorized(ctx, out var unauthorized))
            return unauthorized;

        request ??= new AdminBanAccountRequest();
        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return Error("Account not found", statusCode: StatusCodes.Status404NotFound);

        if (!TryResolveBanExpireAt(request, out var expireAt, out var error))
            return Error(error);

        account.AddKnownIdentityKeys(GetActiveConnectionIdentityKeys(account.Uid));
        account.Ban(request.reason, expireAt);
        ConnectionDisconnectHelper.KickBanned(Listener.GetActiveConnection(uid), account.GetBanStatus());

        return Success(new AccountWrap(ToAccountSummary(account)));
    }

    private static IResult UnbanAccount(HttpContext ctx, int uid)
    {
        if (!IsAuthorized(ctx, out var unauthorized))
            return unauthorized;

        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return Error("Account not found", statusCode: StatusCodes.Status404NotFound);

        account.ClearBan();
        return Success(new AccountWrap(ToAccountSummary(account)));
    }

    private static IResult RevokeAccountTokens(HttpContext ctx, int uid)
    {
        if (!IsAuthorized(ctx, out var unauthorized))
            return unauthorized;

        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return Error("Account not found", statusCode: StatusCodes.Status404NotFound);

        account.ClearLoginTokens();

        return Success(new AccountWrap(ToAccountSummary(account)));
    }

    private static IResult KickAccountSession(HttpContext ctx, int uid)
    {
        if (!IsAuthorized(ctx, out var unauthorized))
            return unauthorized;

        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return Error("Account not found", statusCode: StatusCodes.Status404NotFound);

        var activeConnection = Listener.GetActiveConnection(uid);
        ConnectionDisconnectHelper.KickByGm(activeConnection);

        return Success(new AccountOnlineWrap(ToAccountSummary(account), activeConnection != null));
    }

    private static async Task<IResult> ResetAccountGameplay(HttpContext ctx, int uid)
    {
        if (!IsAuthorized(ctx, out var unauthorized))
            return unauthorized;

        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return Error("Account not found", statusCode: StatusCodes.Status404NotFound);

        if (DatabaseHelper.Instance == null)
            return Error("Database is not initialized");

        var wasOnline = await PlayerResetHelper.ResetGameplayAsync(uid);

        return Success(new AccountOnlineWrap(ToAccountSummary(account), wasOnline));
    }

    private static IResult ClearIdentityKeys(HttpContext ctx, int uid)
    {
        if (!IsAuthorized(ctx, out var unauthorized))
            return unauthorized;

        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return Error("Account not found", statusCode: StatusCodes.Status404NotFound);

        account.KnownIdentityKeys = null;
        DatabaseHelper.MarkDirty(uid);
        DatabaseHelper.SaveDatabaseType(account);

        return Success(new AccountWrap(ToAccountSummary(account)));
    }

    private static IResult DeleteAccount(HttpContext ctx, int uid)
    {
        if (!IsAuthorized(ctx, out var unauthorized))
            return unauthorized;

        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return Error("Account not found", statusCode: StatusCodes.Status404NotFound);

        // Drop the live session first so the client does not keep writing into data that is being removed.
        ConnectionDisconnectHelper.KickByGm(Listener.GetActiveConnection(uid));
        DatabaseHelper.Instance?.DeleteUidData(uid);
        return Success(new DeletedUid(uid));
    }

    private static bool TryResolveBanExpireAt(AdminBanAccountRequest request, out long expireAt, out string error)
    {
        expireAt = 0;
        error = string.Empty;

        if (request.permanent.GetValueOrDefault(true))
            return true;

        var now = Extensions.GetUnixSec();
        if (request.expire_at.HasValue)
        {
            expireAt = request.expire_at.Value;
            if (expireAt <= now)
            {
                error = "Ban expiry must be in the future";
                return false;
            }

            return true;
        }

        if (request.duration_seconds.HasValue)
        {
            if (request.duration_seconds.Value <= 0)
            {
                error = "Ban duration must be greater than zero";
                return false;
            }

            expireAt = now + request.duration_seconds.Value;
            return true;
        }

        if (!string.IsNullOrWhiteSpace(request.duration))
        {
            if (!DurationParser.TryParseSeconds(request.duration, out var seconds) || seconds <= 0)
            {
                error = "Invalid ban duration";
                return false;
            }

            expireAt = now + seconds;
            return true;
        }

        error = "Timed bans require an expiry or duration";
        return false;
    }

    private static IReadOnlyCollection<string> GetActiveConnectionIdentityKeys(int uid)
    {
        var activeConnection = Listener.GetActiveConnection(uid);
        return activeConnection == null
            ? []
            : AccountBanHelper.BuildIdentityKeys([], activeConnection.RemoteEndPoint.Address.ToString());
    }

    public class AdminCreateAccountRequest
    {
        public string? username { get; set; }
        public string? email { get; set; }
        public string? password { get; set; }
        public int? uid { get; set; }
        public string? role { get; set; }
        public string? permissions { get; set; }
        public bool? email_verified { get; set; }
    }

    public class AdminUpdateAccountRequest
    {
        public string? username { get; set; }
        public string? email { get; set; }
        public string? role { get; set; }
        public string? permissions { get; set; }
        public bool? email_verified { get; set; }
    }

    public class AdminSetPasswordRequest
    {
        public string? password { get; set; }
        public bool? revoke_tokens { get; set; }
    }

    public class AdminBanAccountRequest
    {
        public string? reason { get; set; }
        public bool? permanent { get; set; }
        public long? expire_at { get; set; }
        public int? duration_seconds { get; set; }
        public string? duration { get; set; }
    }
}
