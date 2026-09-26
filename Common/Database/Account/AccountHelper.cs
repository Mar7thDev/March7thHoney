using MemoryPack;
using March7thHoney.Database.Mail;
using March7thHoney.Command;
using March7thHoney.Util;
using System.Net.Mail;

namespace March7thHoney.Database.Account;

public static class AccountHelper
{
    private static readonly object CreationLock = new();
    private static int _nextUid = 10001;

    public static AccountData CreatePublicAccount(string username, string email, string password,
        IReadOnlyCollection<string> identityKeys)
    {
        // Keep policy checks, UID allocation and identity persistence in one critical section.
        lock (CreationLock)
        {
            var auth = ConfigManager.Config.ServerOption.Auth;
            if (!auth.EnableRegistration) throw new InvalidOperationException("Registration is disabled");
            if (AccountBanHelper.TryFindActiveBanByIdentityKeys(identityKeys, null, out var ban))
                throw new InvalidOperationException(ban.FormatLoginMessage());
            if (auth.AntiAbuse.RequireRegistrationIdentity && !identityKeys.Any(k => k.StartsWith("device:")))
                throw new InvalidOperationException("Registration requires a device ID");
            if (auth.AntiAbuse.LimitRegistrationPerIdentity && AccountBanHelper.TryFindAccountByIdentityKeys(
                    identityKeys, auth.AntiAbuse.RegistrationIdentityMode, out _))
                throw new InvalidOperationException("Registration is not available for this device or network");
            ValidatePassword(password);
            return CreateAccount(username, email, password, 0, null, identityKeys: identityKeys);
        }
    }

    /// <summary>
    ///     Account created on behalf of the trusted website (WebAccountApi). The website owns sign-up policy
    ///     and email ownership, so the public registration switch and identity limits do not apply, but a
    ///     username may not differ from an existing one only by case: players type it into the game login.
    /// </summary>
    public static AccountData CreateWebAccount(string username, string email, string password)
    {
        lock (CreationLock)
        {
            username = username.Trim();
            var accounts = DatabaseHelper.GetAllInstanceFromMap<AccountData>() ??
                           DatabaseHelper.GetAllInstance<AccountData>() ?? [];
            if (accounts.Any(a => string.Equals(a.Username?.Trim(), username, StringComparison.OrdinalIgnoreCase)))
                throw new Exception("Account already exists");
            ValidatePassword(password);
            return CreateAccountCore(username, email, password, 0, null, true, null);
        }
    }

    public static void CreateAccount(string username, int uid)
    {
        CreateAccount(username, null, uid);
    }

    public static void CreateAccount(string username, string? password, int uid)
    {
        CreateAccount(username, null, password, uid, null);
    }

    public static void CreateAccount(string username, string? password, int uid, string? role)
    {
        CreateAccount(username, null, password, uid, role);
    }

    public static void CreateAccount(string username, string email, string? password, int uid)
    {
        CreateAccount(username, email, password, uid, null);
    }

    public static AccountData CreateAccount(string username, string? email, string? password, int uid, string? role,
        bool emailVerified = false, IReadOnlyCollection<string>? identityKeys = null)
    {
        lock (CreationLock)
            return CreateAccountCore(username, email, password, uid, role, emailVerified, identityKeys);
    }

    private static AccountData CreateAccountCore(string username, string? email, string? password, int uid, string? role,
        bool emailVerified, IReadOnlyCollection<string>? identityKeys)
    {
        username = username.Trim();
        if (string.IsNullOrWhiteSpace(username)) throw new Exception("Username cannot be empty");
        if (username.Length > 64 || username.Any(char.IsControl)) throw new Exception("Invalid username");
        if (uid < 0) throw new Exception("UID must be positive");
        email = NormalizeAndValidateEmail(email);

        if (AccountData.GetAccountByUserName(username) != null) throw new Exception("Account already exists");
        if (AccountData.GetAccountByEmail(email) != null) throw new Exception("Email already exists");
        if (uid != 0 && AccountData.GetAccountByUid(uid) != null) throw new Exception("UID already exists");
        if (uid != 0 && DatabaseHelper.IsUidDeleted(uid)) throw new Exception("UID has been retired");

        var newUid = uid;
        if (uid == 0)
        {
            newUid = _nextUid;
            while (AccountData.GetAccountByUid(newUid) != null || DatabaseHelper.IsUidDeleted(newUid)) newUid++;
            _nextUid = checked(newUid + 1);
        }

        var roleName = CommandRoles.NormalizeRole(role, ConfigManager.Config.ServerOption.DefaultPermissionRole,
            ConfigManager.Config.ServerOption.PermissionRoles);
        var account = new AccountData
        {
            Uid = newUid,
            Username = username,
            Email = email,
            NormalizedEmail = AccountData.NormalizeEmail(email),
            IsEmailVerified = emailVerified,
            EmailVerifiedAt = emailVerified ? Extensions.GetUnixSec() : 0,
            Role = roleName,
            CreatedAt = Extensions.GetUnixSec(),
            RegistrationIp = (identityKeys as LoginIdentityContext)?.IpAddress
        };
        account.SyncPermissionsWithRole(false);
        account.AddKnownIdentityKeys(identityKeys, false);

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
        return account;
    }

    public static void SetPassword(AccountData account, string password)
    {
        ValidatePassword(password);
        account.SetPassword(password);
    }

    public static void SetEmail(AccountData account, string email, bool verified)
    {
        email = NormalizeAndValidateEmail(email);
        var existing = AccountData.GetAccountByEmail(email);
        if (existing != null && existing.Uid != account.Uid) throw new Exception("Email already exists");

        account.SetEmail(email, verified);
    }

    public static string NormalizeAndValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new Exception("Email cannot be empty");

        email = email.Trim();
        if (email.Length > 254) throw new Exception("Email is too long");
        try
        {
            var address = new MailAddress(email);
            if (!string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase))
                throw new Exception();
        }
        catch
        {
            throw new Exception("Email is invalid");
        }

        return email;
    }

    public static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new Exception("Password cannot be empty");
        if (password.Length > 1024) throw new Exception("Password is too long");

        var minLength = Math.Max(ConfigManager.Config.ServerOption.Auth.MinimumPasswordLength, 1);
        if (password.Length < minLength)
            throw new Exception($"Password must be at least {minLength} characters long");
    }
}
