using March7thHoney.Database;
using March7thHoney.Database.Account;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server;
using March7thHoney.Kcp;
using March7thHoney.Util;
using March7thHoney.WebServer.Handler;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static March7thHoney.WebServer.Http.WebResults;

namespace March7thHoney.WebServer.Controllers;

public static partial class AdminRoutes
{
    private static void MapAccountRoutes(IEndpointRouteBuilder api)
    {
        api.MapGet("/accounts", GetAccounts);
        api.MapPost("/accounts", CreateAccount);
        api.MapPost("/accounts/bulk-delete", BulkDeleteAccounts);
        api.MapGet("/identity-blocks", GetIdentityBlocks);
        api.MapPost("/identity-blocks", BlockIdentity);
        api.MapDelete("/identity-blocks/{id:int}", UnblockIdentity);
        api.MapPut("/accounts/{uid:int}", UpdateAccount);
        api.MapPut("/accounts/{uid:int}/password", SetPassword);
        api.MapPost("/accounts/{uid:int}/email/verification", SendAccountVerificationEmail);
        api.MapPost("/accounts/{uid:int}/ban", BanAccount);
        api.MapDelete("/accounts/{uid:int}/ban", UnbanAccount);
        api.MapPost("/accounts/{uid:int}/tokens/revoke", RevokeAccountTokens);
        api.MapPost("/accounts/{uid:int}/session/kick", KickAccountSession);
        api.MapPost("/accounts/{uid:int}/gameplay/reset", ResetAccountGameplay);
        api.MapDelete("/accounts/{uid:int}/identity-keys", ClearIdentityKeys);
        api.MapDelete("/accounts/{uid:int}", DeleteAccount);
    }

    private static IResult GetAccounts()
    {
        var accounts = GetAllAccounts()
            .Select(ToAccountSummary)
            .OrderBy(account => account.uid)
            .ToList();

        return Success(new AccountsData(accounts, accounts.Count, GetRoleNames(), NormalizeRole(null)));
    }

    private static IResult CreateAccount(AdminCreateAccountRequest? request)
    {
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
            emailSent = await AccountRecoveryHandler.SendVerificationEmailForAccount(account);

        return Success(new AccountEmailWrap(ToAccountSummary(account), emailSent));
    }

    private static IResult SetPassword(int uid, AdminSetPasswordRequest? request)
    {
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
        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return Error("Account not found", statusCode: StatusCodes.Status404NotFound);

        if (string.IsNullOrWhiteSpace(account.Email))
            return Error("Account does not have an email address");

        var emailSent = await AccountRecoveryHandler.SendVerificationEmailForAccount(account,
            markUnverified: true);
        return Success(new AccountEmailWrap(ToAccountSummary(account), emailSent));
    }

    private static IResult BanAccount(int uid, AdminBanAccountRequest? request)
    {
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

    private static IResult UnbanAccount(int uid)
    {
        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return Error("Account not found", statusCode: StatusCodes.Status404NotFound);

        account.ClearBan();
        return Success(new AccountWrap(ToAccountSummary(account)));
    }

    private static IResult RevokeAccountTokens(int uid)
    {
        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return Error("Account not found", statusCode: StatusCodes.Status404NotFound);

        account.ClearLoginTokens();

        return Success(new AccountWrap(ToAccountSummary(account)));
    }

    private static IResult KickAccountSession(int uid)
    {
        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return Error("Account not found", statusCode: StatusCodes.Status404NotFound);

        var activeConnection = Listener.GetActiveConnection(uid);
        ConnectionDisconnectHelper.KickByGm(activeConnection);

        return Success(new AccountOnlineWrap(ToAccountSummary(account), activeConnection != null));
    }

    private static async Task<IResult> ResetAccountGameplay(int uid)
    {
        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return Error("Account not found", statusCode: StatusCodes.Status404NotFound);

        if (DatabaseHelper.Instance == null)
            return Error("Database is not initialized");

        var wasOnline = await PlayerResetHelper.ResetGameplayAsync(uid);

        return Success(new AccountOnlineWrap(ToAccountSummary(account), wasOnline));
    }

    private static IResult ClearIdentityKeys(int uid)
    {
        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return Error("Account not found", statusCode: StatusCodes.Status404NotFound);

        account.KnownIdentityKeys = null;
        account.KnownDeviceIds = [];
        account.KnownIpAddresses = [];
        account.LastIp = null;
        DatabaseHelper.MarkDirty(uid);
        DatabaseHelper.SaveDatabaseType(account);

        return Success(new AccountWrap(ToAccountSummary(account)));
    }

    private static async Task<IResult> DeleteAccount(int uid)
    {
        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return Error("Account not found", statusCode: StatusCodes.Status404NotFound);

        await DeleteAccountData(account);
        return Success(new DeletedUid(uid));
    }

    private static async Task DeleteAccountData(AccountData account)
    {
        var database = DatabaseHelper.Instance ?? throw new InvalidOperationException("Database is not initialized");
        if (!DatabaseHelper.LoadAllData) throw new InvalidOperationException("Database is still loading; retry deletion shortly");
        lock (account)
        {
            if (account.IsDeleting) throw new InvalidOperationException("Account deletion is already in progress");
            account.IsDeleting = true;
        }
        try
        {
            account.ClearLoginTokens();
            var connections = March7thHoneyListener.GetSnapshot().OfType<Connection>()
                .Where(c => c.Player?.Uid == account.Uid).ToList();
            await Task.WhenAll(connections.Select(ConnectionDisconnectHelper.KickByGmAsync));
            if (connections.Any(c => c.IsOnline))
                throw new InvalidOperationException("Session could not be stopped; retry deletion");
            database.DeleteUidData(account.Uid);
        }
        finally
        {
            if (!DatabaseHelper.IsUidDeleted(account.Uid)) account.IsDeleting = false;
        }
    }

    private static async Task<IResult> BulkDeleteAccounts(AdminBulkDeleteRequest? request)
    {
        if (request?.confirm != "DELETE") return Error("Deletion confirmation is required");
        if (request.uids == null || request.uids.Count is < 1 or > 500 || request.uids.Any(uid => uid <= 0))
            return Error("Select between 1 and 500 positive UIDs");
        if (DatabaseHelper.Instance == null) return Error("Database is not initialized");

        var deleted = new List<int>();
        var missing = new List<int>();
        var failed = new List<AccountDeleteFailure>();
        foreach (var uid in request.uids.Distinct())
        {
            var account = AccountData.GetAccountByUid(uid);
            if (account == null) { missing.Add(uid); continue; }
            try { await DeleteAccountData(account); deleted.Add(uid); }
            catch (Exception ex) { failed.Add(new AccountDeleteFailure(uid, ex.Message)); }
        }
        return Success(new BulkDeleteData(deleted, missing, failed));
    }

    public class AdminBulkDeleteRequest
    {
        public List<int>? uids { get; set; }
        public string? confirm { get; set; }
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
            if (expireAt <= now || expireAt > DateTimeOffset.MaxValue.ToUnixTimeSeconds())
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
