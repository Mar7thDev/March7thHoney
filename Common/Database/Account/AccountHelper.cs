using March7thHoney.Database.Mail;
using March7thHoney.Command;
using March7thHoney.Util;

namespace March7thHoney.Database.Account;

public static class AccountHelper
{
    public static void CreateAccount(string username, int uid)
    {
        CreateAccount(username, null, uid);
    }

    public static void CreateAccount(string username, string? password, int uid)
    {
        CreateAccount(username, password, uid, null);
    }

    public static void CreateAccount(string username, string? password, int uid, string? role)
    {
        username = username.Trim();
        if (string.IsNullOrWhiteSpace(username)) throw new Exception("Username cannot be empty");
        if (AccountData.GetAccountByUserName(username) != null) throw new Exception("Account already exists");
        if (uid != 0 && AccountData.GetAccountByUid(uid) != null) throw new Exception("UID already exists");

        var newUid = uid;
        if (uid == 0)
        {
            newUid = 10001; // start from 10001
            while (AccountData.GetAccountByUid(newUid) != null) newUid++;
        }

        var roleName = CommandRoles.NormalizeRole(role, ConfigManager.Config.ServerOption.DefaultPermissionRole,
            ConfigManager.Config.ServerOption.PermissionRoles);
        var account = new AccountData
        {
            Uid = newUid,
            Username = username,
            Role = roleName
        };
        account.SyncPermissionsWithRole(false);

        if (!string.IsNullOrWhiteSpace(password))
        {
            ValidatePassword(password);
            var (salt, hash, iterations) = March7thHoney.Util.Security.AuthSecurity.HashPassword(password);
            account.PasswordSalt = salt;
            account.PasswordHash = hash;
            account.PasswordIterations = iterations;
        }

        DatabaseHelper.SaveInstance(account);
        MailHelper.SendWelcomeMail(newUid, username);
    }

    public static void SetPassword(AccountData account, string password)
    {
        ValidatePassword(password);
        account.SetPassword(password);
    }

    public static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new Exception("Password cannot be empty");

        var minLength = Math.Max(ConfigManager.Config.ServerOption.Auth.MinimumPasswordLength, 1);
        if (password.Length < minLength)
            throw new Exception($"Password must be at least {minLength} characters long");
    }
}
