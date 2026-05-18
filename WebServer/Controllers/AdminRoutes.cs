using March7thHoney.Database;
using March7thHoney.Database.Account;
using March7thHoney.Database.Player;
using March7thHoney.Util;
using March7thHoney.WebServer.Handler;
using March7thHoney.WebServer.Pages;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace March7thHoney.WebServer.Controllers;

[ApiController]
[EnableCors("AllowAll")]
[Route("/")]
public class AdminRoutes : ControllerBase
{
    [HttpGet("/admin")]
    [HttpGet("/admin/")]
    public IActionResult AdminPageRoot()
    {
        return Redirect("/admin/index.html");
    }

    [HttpGet("/admin/index.html")]
    public ContentResult AdminPage()
    {
        return new ContentResult
        {
            Content = AdminPageRenderer.Render(ConfigManager.Config.GameServer.GameServerName),
            ContentType = "text/html; charset=utf-8"
        };
    }

    [HttpPost("/admin/api/login")]
    public JsonResult Login([FromBody] AdminLoginRequest? request)
    {
        request ??= new AdminLoginRequest();
        if (!AdminSessionManager.TryCreateSession(request.admin_key ?? string.Empty, out var session, out var message))
            return Error(message, statusCode: StatusCodes.Status401Unauthorized);

        return Success(new
        {
            token = session.Token,
            expires_at = session.ExpiresAt.ToUnixTimeSeconds()
        });
    }

    [HttpPost("/admin/api/logout")]
    public JsonResult Logout()
    {
        AdminSessionManager.Revoke(GetAdminToken());
        return Success(new { });
    }

    [HttpGet("/admin/api/accounts")]
    public JsonResult GetAccounts()
    {
        if (!IsAuthorized(out var unauthorized))
            return unauthorized;

        var accounts = GetAllAccounts()
            .Select(ToAccountSummary)
            .OrderBy(account => account.uid)
            .ToList();

        return Success(new
        {
            accounts,
            count = accounts.Count
        });
    }

    [HttpPost("/admin/api/accounts")]
    public JsonResult CreateAccount([FromBody] AdminCreateAccountRequest? request)
    {
        if (!IsAuthorized(out var unauthorized))
            return unauthorized;

        request ??= new AdminCreateAccountRequest();
        var username = (request.username ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(username))
            return Error("Username cannot be empty");

        if (string.IsNullOrWhiteSpace(request.password))
            return Error("Password cannot be empty");

        var uid = request.uid.GetValueOrDefault();
        try
        {
            AccountHelper.CreateAccount(username, request.password, uid);
            var account = AccountData.GetAccountByUserName(username);
            if (account == null)
                return Error("Failed to create account");

            if (!string.IsNullOrWhiteSpace(request.permissions))
            {
                account.Permissions = NormalizePermissions(request.permissions);
                DatabaseHelper.SaveDatabaseType(account);
            }

            return Success(new
            {
                account = ToAccountSummary(account)
            });
        }
        catch (Exception ex)
        {
            return Error(ex.Message);
        }
    }

    [HttpPut("/admin/api/accounts/{uid:int}")]
    public JsonResult UpdateAccount([FromRoute] int uid, [FromBody] AdminUpdateAccountRequest? request)
    {
        if (!IsAuthorized(out var unauthorized))
            return unauthorized;

        request ??= new AdminUpdateAccountRequest();
        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return Error("Account not found", statusCode: StatusCodes.Status404NotFound);

        var username = (request.username ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(username))
            return Error("Username cannot be empty");

        var existing = AccountData.GetAccountByUserName(username);
        if (existing != null && existing.Uid != uid)
            return Error("Username already exists");

        account.Username = username;
        account.Permissions = NormalizePermissions(request.permissions);
        DatabaseHelper.SaveDatabaseType(account);

        return Success(new
        {
            account = ToAccountSummary(account)
        });
    }

    [HttpPut("/admin/api/accounts/{uid:int}/password")]
    public JsonResult SetPassword([FromRoute] int uid, [FromBody] AdminSetPasswordRequest? request)
    {
        if (!IsAuthorized(out var unauthorized))
            return unauthorized;

        request ??= new AdminSetPasswordRequest();
        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return Error("Account not found", statusCode: StatusCodes.Status404NotFound);

        try
        {
            AccountHelper.SetPassword(account, request.password ?? string.Empty);
            return Success(new
            {
                account = ToAccountSummary(account)
            });
        }
        catch (Exception ex)
        {
            return Error(ex.Message);
        }
    }

    [HttpDelete("/admin/api/accounts/{uid:int}")]
    public JsonResult DeleteAccount([FromRoute] int uid)
    {
        if (!IsAuthorized(out var unauthorized))
            return unauthorized;

        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return Error("Account not found", statusCode: StatusCodes.Status404NotFound);

        DatabaseHelper.Instance?.DeleteUidData(uid);
        return Success(new { uid });
    }

    private bool IsAuthorized(out JsonResult error)
    {
        if (AdminSessionManager.IsAuthorized(GetAdminToken()))
        {
            error = Success(new { });
            return true;
        }

        error = Error("Not authorized", statusCode: StatusCodes.Status401Unauthorized);
        return false;
    }

    private string? GetAdminToken()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return authorization["Bearer ".Length..].Trim();

        return Request.Headers["X-Admin-Session"].FirstOrDefault();
    }

    private static List<AccountData> GetAllAccounts()
    {
        return (DatabaseHelper.GetAllInstanceFromMap<AccountData>() ?? DatabaseHelper.GetAllInstance<AccountData>() ?? [])
            .GroupBy(account => account.Uid)
            .Select(group => group.First())
            .ToList();
    }

    private static AccountSummary ToAccountSummary(AccountData account)
    {
        var player = DatabaseHelper.Instance?.GetInstance<PlayerData>(account.Uid);
        return new AccountSummary(
            account.Uid,
            account.Username ?? "",
            player?.Name ?? "",
            player?.Level,
            account.HasPassword(),
            account.Permissions ?? "",
            account.DispatchTokenExpireAt,
            account.ComboTokenExpireAt);
    }

    private static string NormalizePermissions(string? permissions)
    {
        return string.Join(",", (permissions ?? string.Empty)
            .Split([',', '\r', '\n', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal));
    }

    private static JsonResult Success(object data)
    {
        return new JsonResult(new { retcode = 0, message = "OK", data });
    }

    private static JsonResult Error(string message, int retcode = -201, int statusCode = StatusCodes.Status400BadRequest)
    {
        return new JsonResult(new { retcode, message, data = (object?)null })
        {
            StatusCode = statusCode
        };
    }

    public class AdminLoginRequest
    {
        public string? admin_key { get; set; }
    }

    public class AdminCreateAccountRequest
    {
        public string? username { get; set; }
        public string? password { get; set; }
        public int? uid { get; set; }
        public string? permissions { get; set; }
    }

    public class AdminUpdateAccountRequest
    {
        public string? username { get; set; }
        public string? permissions { get; set; }
    }

    public class AdminSetPasswordRequest
    {
        public string? password { get; set; }
    }

    private readonly record struct AccountSummary(
        int uid,
        string username,
        string nickname,
        int? level,
        bool has_password,
        string permissions,
        long dispatch_token_expires_at,
        long combo_token_expires_at);
}
