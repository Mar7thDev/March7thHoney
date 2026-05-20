using System.Diagnostics;
using System.Text;
using March7thHoney.Database;
using March7thHoney.Database.Account;
using March7thHoney.Database.Mail;
using March7thHoney.Database.Player;
using March7thHoney.Command;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server;
using March7thHoney.Kcp;
using March7thHoney.Util;
using March7thHoney.WebServer.Handler;
using March7thHoney.WebServer.Pages;
using March7thHoney.WebServer.Server;
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
            count = accounts.Count,
            roles = GetRoleNames(),
            default_role = CommandRoles.NormalizeRole(null, ConfigManager.Config.ServerOption.DefaultPermissionRole,
                GetConfiguredRoles())
        });
    }

    [HttpGet("/admin/api/overview")]
    public JsonResult GetOverview()
    {
        if (!IsAuthorized(out var unauthorized))
            return unauthorized;

        var accounts = GetAllAccounts()
            .Select(ToAccountSummary)
            .OrderBy(account => account.uid)
            .ToList();
        var onlinePlayers = GetOnlinePlayers();
        var process = Process.GetCurrentProcess();

        return Success(new
        {
            stats = new
            {
                total_accounts = accounts.Count,
                online_players = onlinePlayers.Count,
                banned_accounts = accounts.Count(account => account.banned),
                unverified_email = accounts.Count(account => !account.email_verified),
                roles = GetConfiguredRoles().Count
            },
            server = new
            {
                name = ConfigManager.Config.GameServer.GameServerName,
                game_address = ConfigManager.Config.GameServer.GetDisplayAddress(),
                database = ConfigManager.Config.Database.DatabaseType,
                uptime_seconds = Math.Max(0,
                    (long)(DateTime.Now - process.StartTime).TotalSeconds),
                process_memory_mb = Math.Round(process.WorkingSet64 / 1024d / 1024d, 1),
                server_time = Extensions.GetUnixSec()
            },
            online_players = onlinePlayers,
            role_breakdown = accounts
                .GroupBy(account => account.role)
                .Select(group => new { role = group.Key, count = group.Count() })
                .OrderBy(item => item.role)
                .ToList(),
            attention = accounts
                .Where(account => account.banned || !account.email_verified || !account.has_password)
                .Select(account => new
                {
                    account.uid,
                    account.username,
                    account.email,
                    account.role,
                    reason = account.banned
                        ? "Banned"
                        : !account.email_verified
                            ? "Email unverified"
                            : "Password missing"
                })
                .Take(8)
                .ToList()
        });
    }

    [HttpGet("/admin/api/access")]
    public JsonResult GetAccess()
    {
        if (!IsAuthorized(out var unauthorized))
            return unauthorized;

        var serverOption = ConfigManager.Config.ServerOption;
        var roles = GetConfiguredRoles()
            .Select(role => new
            {
                name = role.Key,
                configured_permissions = role.Value.OrderBy(permission => permission).ToList(),
                effective_permissions = CommandRoles
                    .GetPermissions(role.Key, serverOption.DefaultPermissionRole, GetConfiguredRoles())
                    .OrderBy(permission => permission)
                    .ToList()
            })
            .OrderBy(role => role.name)
            .ToList();

        return Success(new
        {
            roles,
            default_role = NormalizeRole(null),
            default_permissions = serverOption.DefaultPermissions
                .OrderBy(permission => permission)
                .ToList(),
            known_permissions = CommandPermissions.KnownPermissions()
                .OrderBy(permission => permission)
                .ToList()
        });
    }

    [HttpPost("/admin/api/commands")]
    public JsonResult ExecuteCommand([FromBody] AdminExecuteCommandRequest? request)
    {
        if (!IsAuthorized(out var unauthorized))
            return unauthorized;

        request ??= new AdminExecuteCommandRequest();
        var command = (request.command ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(command))
            return Error("Command cannot be empty");

        var sessionId = $"admin-page-{Guid.NewGuid():N}";
        MuipManager.Sessions[sessionId] = new MuipSession
        {
            SessionId = sessionId,
            ExpireTimeStamp = DateTime.Now.AddMinutes(1).ToUnixSec(),
            IsAdmin = true,
            IsAuthorized = true
        };

        try
        {
            var response = MuipManager.ExecuteCommand(sessionId, command, request.target_uid.GetValueOrDefault());
            if (response.Code != 0)
                return Error(response.Message);

            var output = string.Empty;
            if (!string.IsNullOrWhiteSpace(response.Data?.Message))
                output = Encoding.UTF8.GetString(Convert.FromBase64String(response.Data.Message));

            return Success(new
            {
                output = output.TrimEnd(),
                target_uid = request.target_uid.GetValueOrDefault()
            });
        }
        catch (Exception ex)
        {
            return Error(ex.Message);
        }
        finally
        {
            MuipManager.Sessions.Remove(sessionId);
        }
    }

    [HttpPost("/admin/api/accounts")]
    public JsonResult CreateAccount([FromBody] AdminCreateAccountRequest? request)
    {
        if (!IsAuthorized(out var unauthorized))
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
    public async Task<JsonResult> UpdateAccount([FromRoute] int uid, [FromBody] AdminUpdateAccountRequest? request)
    {
        if (!IsAuthorized(out var unauthorized))
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
            emailSent = await AccountRecoveryHandler.SendVerificationEmailForAccount(account, Request);

        return Success(new
        {
            account = ToAccountSummary(account),
            email_sent = emailSent
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

    [HttpPost("/admin/api/accounts/{uid:int}/email/verification")]
    public async Task<JsonResult> SendAccountVerificationEmail([FromRoute] int uid)
    {
        if (!IsAuthorized(out var unauthorized))
            return unauthorized;

        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return Error("Account not found", statusCode: StatusCodes.Status404NotFound);

        if (string.IsNullOrWhiteSpace(account.Email))
            return Error("Account does not have an email address");

        var emailSent = await AccountRecoveryHandler.SendVerificationEmailForAccount(account, Request,
            markUnverified: true);
        return Success(new
        {
            account = ToAccountSummary(account),
            email_sent = emailSent
        });
    }

    [HttpPost("/admin/api/accounts/{uid:int}/ban")]
    public JsonResult BanAccount([FromRoute] int uid, [FromBody] AdminBanAccountRequest? request)
    {
        if (!IsAuthorized(out var unauthorized))
            return unauthorized;

        request ??= new AdminBanAccountRequest();
        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return Error("Account not found", statusCode: StatusCodes.Status404NotFound);

        if (!TryResolveBanExpireAt(request, out var expireAt, out var error))
            return Error(error);

        account.AddKnownIdentityKeys(GetActiveConnectionIdentityKeys(account.Uid));
        account.Ban(request.reason, expireAt);
        KickBannedConnection(account.Uid, account.GetBanStatus());

        return Success(new
        {
            account = ToAccountSummary(account)
        });
    }

    [HttpDelete("/admin/api/accounts/{uid:int}/ban")]
    public JsonResult UnbanAccount([FromRoute] int uid)
    {
        if (!IsAuthorized(out var unauthorized))
            return unauthorized;

        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return Error("Account not found", statusCode: StatusCodes.Status404NotFound);

        account.ClearBan();
        return Success(new
        {
            account = ToAccountSummary(account)
        });
    }

    [HttpPost("/admin/api/accounts/{uid:int}/tokens/revoke")]
    public JsonResult RevokeAccountTokens([FromRoute] int uid)
    {
        if (!IsAuthorized(out var unauthorized))
            return unauthorized;

        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return Error("Account not found", statusCode: StatusCodes.Status404NotFound);

        account.ClearLoginTokens();

        return Success(new
        {
            account = ToAccountSummary(account)
        });
    }

    [HttpPost("/admin/api/accounts/{uid:int}/session/kick")]
    public JsonResult KickAccountSession([FromRoute] int uid)
    {
        if (!IsAuthorized(out var unauthorized))
            return unauthorized;

        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return Error("Account not found", statusCode: StatusCodes.Status404NotFound);

        var activeConnection = Listener.GetActiveConnection(uid);
        ConnectionDisconnectHelper.KickByGm(activeConnection);

        return Success(new
        {
            account = ToAccountSummary(account),
            was_online = activeConnection != null
        });
    }

    [HttpPost("/admin/api/accounts/{uid:int}/gameplay/reset")]
    public JsonResult ResetAccountGameplay([FromRoute] int uid)
    {
        if (!IsAuthorized(out var unauthorized))
            return unauthorized;

        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return Error("Account not found", statusCode: StatusCodes.Status404NotFound);

        if (DatabaseHelper.Instance == null)
            return Error("Database is not initialized");

        var activeConnection = Listener.GetActiveConnection(uid);
        ConnectionDisconnectHelper.KickByGm(activeConnection);

        DatabaseHelper.Instance.DeleteUidGameplayData(uid);

        var freshPlayer = new PlayerInstance(uid);
        freshPlayer.Data.Level = ConfigManager.Config.ServerOption.StartTrailblazerLevel;
        freshPlayer.Data.Exp = 0;
        freshPlayer.OnLevelChange();

        MailHelper.SendWelcomeMail(uid, account.Username ?? uid.ToString());
        DatabaseHelper.ToSaveUidList.SafeAdd(uid);
        DatabaseHelper.Instance.SaveDatabase();

        return Success(new
        {
            account = ToAccountSummary(account),
            was_online = activeConnection != null
        });
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
        var role = account.GetRole();
        var permissionOverrides = account.GetPermissionOverrides();
        var basePermissions = account.GetBasePermissions();
        var effectivePermissions = account.GetEffectivePermissions();
        var banStatus = account.GetBanStatus();
        return new AccountSummary(
            account.Uid,
            account.Username ?? "",
            account.Email ?? "",
            account.IsEmailVerified,
            account.EmailVerifiedAt,
            player?.Name ?? "",
            player?.Level,
            account.HasPassword(),
            role,
            permissionOverrides,
            basePermissions,
            effectivePermissions,
            account.DispatchTokenExpireAt,
            account.ComboTokenExpireAt,
            Listener.GetActiveConnection(account.Uid) != null,
            banStatus.IsActive,
            banStatus.Reason,
            banStatus.ExpireAt,
            banStatus.CreatedAt,
            banStatus.IsPermanent,
            banStatus.RemainingSeconds);
    }

    private static List<object> GetOnlinePlayers()
    {
        return March7thHoneyListener.Connections.Values
            .OfType<Connection>()
            .Where(connection => connection.Player != null && connection.State == SessionStateEnum.ACTIVE)
            .Select(connection => new
            {
                uid = connection.Player!.Uid,
                nickname = connection.Player.Data.Name ?? "",
                level = connection.Player.Data.Level,
                endpoint = connection.RemoteEndPoint.ToString(),
                state = connection.State.ToString()
            })
            .Cast<object>()
            .ToList();
    }

    private static string NormalizePermissions(string? permissions)
    {
        return CommandPermissions.Normalize(permissions);
    }

    private static string NormalizeRole(string? role)
    {
        return CommandRoles.NormalizeRole(role, ConfigManager.Config.ServerOption.DefaultPermissionRole,
            GetConfiguredRoles());
    }

    private static List<string> GetRoleNames()
    {
        return GetConfiguredRoles().Keys
            .OrderBy(role => role == CommandRoles.User ? 0 :
                role == CommandRoles.Moderator ? 1 :
                role == CommandRoles.Administrator ? 2 :
                role == CommandRoles.Developer ? 3 : 4)
            .ThenBy(role => role, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyDictionary<string, HashSet<string>> GetConfiguredRoles()
    {
        return ConfigManager.Config.ServerOption.PermissionRoles ?? CommandRoles.DefaultRoles();
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

    private static void KickBannedConnection(int uid, AccountBanStatus banStatus)
    {
        ConnectionDisconnectHelper.KickBanned(Listener.GetActiveConnection(uid), banStatus);
    }

    public class AdminLoginRequest
    {
        public string? admin_key { get; set; }
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
    }

    public class AdminBanAccountRequest
    {
        public string? reason { get; set; }
        public bool? permanent { get; set; }
        public long? expire_at { get; set; }
        public int? duration_seconds { get; set; }
    }

    public class AdminExecuteCommandRequest
    {
        public string? command { get; set; }
        public int? target_uid { get; set; }
    }

    private readonly record struct AccountSummary(
        int uid,
        string username,
        string email,
        bool email_verified,
        long email_verified_at,
        string nickname,
        int? level,
        bool has_password,
        string role,
        string permissions,
        string base_permissions,
        string effective_permissions,
        long dispatch_token_expires_at,
        long combo_token_expires_at,
        bool online,
        bool banned,
        string ban_reason,
        long ban_expires_at,
        long ban_created_at,
        bool ban_permanent,
        long ban_remaining_seconds);
}
