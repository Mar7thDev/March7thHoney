using March7thHoney.Command;
using March7thHoney.Util;
using March7thHoney.Util.Security;
using SqlSugar;

namespace March7thHoney.Database.Account;

[SugarTable("Account")]
public class AccountData : BaseDatabaseDataHelper
{
    public string? Username { get; set; }
    [SugarColumn(IsNullable = true)] public string? PasswordSalt { get; set; }
    [SugarColumn(IsNullable = true)] public string? PasswordHash { get; set; }
    public int PasswordIterations { get; set; }

    [SugarColumn(IsNullable = true)] public string? ComboToken { get; set; }
    public long ComboTokenExpireAt { get; set; }

    [SugarColumn(IsNullable = true)] public string? DispatchToken { get; set; }
    public long DispatchTokenExpireAt { get; set; }

    [SugarColumn(IsNullable = true)]
    public string? Role { get; set; }

    [SugarColumn(IsNullable = true)]
    public string? Permissions { get; set; } // type: permission1,permission2,permission3...

    public static AccountData? GetAccountByUserName(string username)
    {
        var normalized = username.Trim();
        AccountData? result = null;
        var accounts = DatabaseHelper.GetAllInstanceFromMap<AccountData>() ?? DatabaseHelper.GetAllInstance<AccountData>();
        accounts?.ForEach(account =>
        {
            if (string.Equals(account.Username?.Trim(), normalized, StringComparison.Ordinal)) result = account;
        });
        return result;
    }

    public static AccountData? GetAccountByUid(int uid)
    {
        var result = DatabaseHelper.Instance?.GetInstance<AccountData>(uid);
        return result;
    }

    public bool HasPassword()
    {
        return !string.IsNullOrWhiteSpace(PasswordSalt) && !string.IsNullOrWhiteSpace(PasswordHash);
    }

    public void SetPassword(string password)
    {
        var (salt, hash, iterations) = AuthSecurity.HashPassword(password);
        PasswordSalt = salt;
        PasswordHash = hash;
        PasswordIterations = iterations;
        PersistAuthState();
    }

    public string GetRole()
    {
        var serverOption = ConfigManager.Config.ServerOption;
        return CommandRoles.NormalizeRole(Role, serverOption.DefaultPermissionRole, serverOption.PermissionRoles);
    }

    public string GetEffectivePermissions()
    {
        var serverOption = ConfigManager.Config.ServerOption;
        var rolePermissions = CommandRoles.GetPermissions(Role, serverOption.DefaultPermissionRole,
            serverOption.PermissionRoles);
        return CommandPermissions.Normalize(rolePermissions.Concat(CommandPermissions.Parse(Permissions)));
    }

    public bool HasPermission(string permission)
    {
        return CommandPermissions.Allows(GetEffectivePermissions(), permission);
    }

    public bool VerifyPassword(string password)
    {
        return AuthSecurity.VerifyPassword(password, PasswordSalt, PasswordHash, PasswordIterations);
    }

    public string GenerateDispatchToken()
    {
        DispatchToken = AuthSecurity.GenerateSessionToken();
        DispatchTokenExpireAt = Extensions.GetUnixSec() +
                                Math.Max(ConfigManager.Config.ServerOption.Auth.DispatchTokenExpireMinutes, 1) * 60L;
        PersistAuthState();
        return DispatchToken;
    }

    public string GenerateComboToken()
    {
        ComboToken = AuthSecurity.GenerateSessionToken();
        ComboTokenExpireAt = Extensions.GetUnixSec() +
                             Math.Max(ConfigManager.Config.ServerOption.Auth.ComboTokenExpireMinutes, 1) * 60L;
        PersistAuthState();
        return ComboToken;
    }

    public bool ValidateDispatchToken(string? token)
    {
        return ValidateToken(token, DispatchToken, DispatchTokenExpireAt);
    }

    public bool ValidateComboToken(string? token)
    {
        return ValidateToken(token, ComboToken, ComboTokenExpireAt);
    }

    public bool ValidateGameToken(string? token)
    {
        return ValidateDispatchToken(token) || ValidateComboToken(token);
    }

    private static bool ValidateToken(string? providedToken, string? storedToken, long expireAt)
    {
        if (string.IsNullOrWhiteSpace(providedToken) ||
            string.IsNullOrWhiteSpace(storedToken) ||
            expireAt <= Extensions.GetUnixSec())
        {
            return false;
        }

        return string.Equals(storedToken, providedToken, StringComparison.Ordinal);
    }

    private void PersistAuthState()
    {
        DatabaseHelper.ToSaveUidList.SafeAdd(Uid);
        DatabaseHelper.SaveDatabaseType(this);
    }
}
