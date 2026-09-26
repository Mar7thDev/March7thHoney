using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using March7thHoney.Database;
using March7thHoney.Database.Account;
using March7thHoney.Database.Player;
using March7thHoney.GameServer.Server;
using March7thHoney.Kcp;
using March7thHoney.Util;
using March7thHoney.WebServer.Handler;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace March7thHoney.WebServer.Controllers;

/// <summary>
///     Game-account provisioning for the trusted website (cyrene-web). The website runs sign-up, email
///     verification and invites; this API only turns a verified website user into a game login.
///     <para>
///         Guarded by <c>ServerOption.Auth.WebAccountApi</c>: disabled answers 404, a caller outside
///         AllowedAddresses or without the shared secret answers 401. Errors carry a stable <c>error</c>
///         code the website maps to its own messages.
///     </para>
/// </summary>
public static partial class WebAccountRoutes
{
    public const string SecretHeader = "X-March7thHoney-Web-Secret";
    private const string Prefix = "/external/web-account/v1";

    public static void MapWebAccountRoutes(this IEndpointRouteBuilder app)
    {
        app.MapGet($"{Prefix}/status", GetStatus);
        app.MapPost($"{Prefix}/accounts", CreateAccount);
        app.MapGet($"{Prefix}/accounts/{{uid:int}}", GetAccount);
        app.MapPut($"{Prefix}/accounts/{{uid:int}}/password", SetPassword);
        app.MapPost($"{Prefix}/accounts/{{uid:int}}/ban", BanAccount);
        app.MapDelete($"{Prefix}/accounts/{{uid:int}}/ban", UnbanAccount);
    }

    // Game logins are typed on a controller-unfriendly login box and compared case-insensitively
    // for uniqueness, so keep them to plain ASCII.
    [GeneratedRegex("^[A-Za-z0-9_.-]{3,32}$")]
    private static partial Regex UsernamePattern();

    private static IResult GetStatus(HttpContext ctx)
    {
        if (!IsAuthorized(ctx, out var denied)) return denied;
        var accounts = DatabaseHelper.GetAllInstanceFromMap<AccountData>() ?? [];
        return Results.Json(new WebAccountResult<WebAccountStatus>(0, "OK", new WebAccountStatus(
            ConfigManager.Config.GameServer.GameServerName, GameConstants.GAME_VERSION, accounts.Count,
            March7thHoneyListener.GetSnapshot().Count, DatabaseHelper.LoadAllData)));
    }

    private static IResult CreateAccount(HttpContext ctx, WebCreateAccountRequest? request)
    {
        if (!IsAuthorized(ctx, out var denied)) return denied;
        if (!DatabaseHelper.LoadAllData) return Error("not_ready", "Database is still loading", 503);

        request ??= new WebCreateAccountRequest();
        var username = (request.username ?? "").Trim();
        if (!UsernamePattern().IsMatch(username))
            return Error("invalid_username", "Username must be 3-32 characters of A-Z, 0-9, _ . -");

        var password = request.password ?? "";
        try
        {
            AccountHelper.ValidatePassword(password);
        }
        catch (Exception ex)
        {
            return Error("invalid_password", ex.Message);
        }

        // The website may omit the address (e.g. a second game account for the same person, whose real
        // address is already taken by the first); the server still needs a unique one.
        var email = string.IsNullOrWhiteSpace(request.email)
            ? $"{username.ToLowerInvariant()}@web-account.invalid"
            : request.email.Trim();
        try
        {
            email = AccountHelper.NormalizeAndValidateEmail(email);
        }
        catch (Exception ex)
        {
            return Error("invalid_email", ex.Message);
        }

        if (AccountData.GetAccountByEmail(email) != null)
            return Error("email_taken", "Email already exists", StatusCodes.Status409Conflict);

        AccountData account;
        try
        {
            account = AccountHelper.CreateWebAccount(username, email, password);
        }
        catch (Exception ex) when (ex.Message is "Account already exists")
        {
            return Error("username_taken", "Username already exists", StatusCodes.Status409Conflict);
        }
        catch (Exception ex) when (ex.Message is "Email already exists")
        {
            return Error("email_taken", "Email already exists", StatusCodes.Status409Conflict);
        }
        catch (Exception ex)
        {
            return Error("create_failed", ex.Message, StatusCodes.Status500InternalServerError);
        }

        return Success(ToInfo(account));
    }

    private static IResult GetAccount(HttpContext ctx, int uid)
    {
        if (!IsAuthorized(ctx, out var denied)) return denied;
        var account = AccountData.GetAccountByUid(uid);
        return account == null ? NotFound() : Success(ToInfo(account));
    }

    private static IResult SetPassword(HttpContext ctx, int uid, WebSetPasswordRequest? request)
    {
        if (!IsAuthorized(ctx, out var denied)) return denied;
        var account = AccountData.GetAccountByUid(uid);
        if (account == null) return NotFound();

        try
        {
            AccountHelper.SetPassword(account, request?.password ?? "");
        }
        catch (Exception ex)
        {
            return Error("invalid_password", ex.Message);
        }

        // A password change from the website is usually "someone else knows it": sign every client out.
        if (request?.revoke_tokens ?? true) account.ClearLoginTokens();
        return Success(ToInfo(account));
    }

    private static IResult BanAccount(HttpContext ctx, int uid, WebBanRequest? request)
    {
        if (!IsAuthorized(ctx, out var denied)) return denied;
        var account = AccountData.GetAccountByUid(uid);
        if (account == null) return NotFound();

        var reason = string.IsNullOrWhiteSpace(request?.reason) ? "Banned on the website" : request.reason.Trim();
        account.Ban(reason, 0);
        ConnectionDisconnectHelper.KickBanned(Listener.GetActiveConnection(uid), account.GetBanStatus());
        return Success(ToInfo(account));
    }

    private static IResult UnbanAccount(HttpContext ctx, int uid)
    {
        if (!IsAuthorized(ctx, out var denied)) return denied;
        var account = AccountData.GetAccountByUid(uid);
        if (account == null) return NotFound();

        account.ClearBan();
        return Success(ToInfo(account));
    }

    private static WebAccountInfo ToInfo(AccountData account)
    {
        var player = DatabaseHelper.Instance?.GetInstance<PlayerData>(account.Uid);
        var ban = account.GetBanStatus();
        return new WebAccountInfo(account.Uid, account.Username ?? "", account.Email ?? "", player?.Name,
            player?.Level, ban.IsActive, ban.Reason, Listener.GetActiveConnection(account.Uid) != null,
            account.CreatedAt, account.LastSeenAt);
    }

    private static bool IsAuthorized(HttpContext ctx, out IResult denied)
    {
        var options = ConfigManager.Config.ServerOption.Auth.WebAccountApi;
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.Secret))
        {
            denied = Results.StatusCode(StatusCodes.Status404NotFound);
            return false;
        }

        denied = Error("unauthorized", "Unauthorized", StatusCodes.Status401Unauthorized);
        if (!IsAllowedAddress(ctx.Connection.RemoteIpAddress, options.AllowedAddresses)) return false;
        if (!ctx.Request.Headers.TryGetValue(SecretHeader, out var provided)) return false;

        var expectedBytes = Encoding.UTF8.GetBytes(options.Secret);
        var providedBytes = Encoding.UTF8.GetBytes(provided.ToString());
        return expectedBytes.Length == providedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }

    private static bool IsAllowedAddress(IPAddress? remote, List<string>? allowed)
    {
        // An empty list means "any address" — the secret is then the only guard.
        if (allowed == null || allowed.Count == 0) return true;
        if (remote == null) return false;
        if (remote.IsIPv4MappedToIPv6) remote = remote.MapToIPv4();
        foreach (var entry in allowed)
        {
            if (!IPAddress.TryParse(entry, out var ip)) continue;
            if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
            if (ip.Equals(remote)) return true;
        }

        return false;
    }

    private static IResult Success(WebAccountInfo account)
    {
        return Results.Json(new WebAccountResult<WebAccountWrap>(0, "OK", new WebAccountWrap(account)));
    }

    private static IResult NotFound()
    {
        return Error("not_found", "Account not found", StatusCodes.Status404NotFound);
    }

    private static IResult Error(string code, string message, int statusCode = StatusCodes.Status400BadRequest)
    {
        return Results.Json(new WebAccountError(-1, message, code), statusCode: statusCode);
    }

    public class WebCreateAccountRequest
    {
        public string? username { get; set; }
        public string? email { get; set; }
        public string? password { get; set; }
    }

    public class WebSetPasswordRequest
    {
        public string? password { get; set; }
        public bool? revoke_tokens { get; set; }
    }

    public class WebBanRequest
    {
        public string? reason { get; set; }
    }
}
