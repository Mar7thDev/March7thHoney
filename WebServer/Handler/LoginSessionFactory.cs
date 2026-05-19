using March7thHoney.Database;
using March7thHoney.Database.Account;
using March7thHoney.Util;

namespace March7thHoney.WebServer.Handler;

internal static class LoginSessionFactory
{
    public const string InvalidCredentialsMessage = "Incorrect account or password";
    public static string EmailVerificationRequiredMessage =>
        ConfigManager.Config.ServerOption.Auth.EmailVerificationRequiredMessage;

    public static bool TryCreateSession(string? account, string? password, out LoginSession session, out LoginFailure failure)
    {
        return TryCreateSession(account, password, [], out session, out failure);
    }

    public static bool TryCreateSession(string? account, string? password, IReadOnlyCollection<string> identityKeys,
        out LoginSession session, out LoginFailure failure)
    {
        session = default;
        failure = LoginFailure.InvalidCredentials();

        var normalizedAccount = (account ?? string.Empty).Trim();
        var normalizedPassword = password ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedAccount) || string.IsNullOrWhiteSpace(normalizedPassword))
            return false;

        var accountData = AccountData.GetAccountByLoginIdentifier(normalizedAccount);
        if (accountData == null)
            return false;

        var canonicalAccount = AccountData.GetAccountByUid(accountData.Uid) ?? accountData;
        if (!canonicalAccount.HasPassword())
        {
            if (!CanAuthenticate(canonicalAccount, identityKeys, out failure))
                return false;

            try
            {
                AccountHelper.SetPassword(canonicalAccount, normalizedPassword);
            }
            catch (Exception ex)
            {
                failure = new LoginFailure(-201, ex.Message);
                return false;
            }
        }
        else if (!canonicalAccount.VerifyPassword(normalizedPassword))
        {
            return false;
        }

        if (canonicalAccount.GetBanStatus().IsActive)
            canonicalAccount.AddKnownIdentityKeys(identityKeys, persist: false);

        if (!CanAuthenticate(canonicalAccount, identityKeys, out failure))
            return false;

        var username = canonicalAccount.Username ?? normalizedAccount;
        canonicalAccount.AddKnownIdentityKeys(identityKeys, persist: false);
        canonicalAccount.SyncPermissionsWithRole();
        var dispatchToken = canonicalAccount.GenerateDispatchToken();
        session = new LoginSession(
            canonicalAccount.Uid.ToString(),
            username,
            canonicalAccount.Email ?? string.Empty,
            canonicalAccount.IsEmailVerified,
            dispatchToken);
        return true;
    }

    public static bool CanAuthenticate(AccountData account, out LoginFailure failure)
    {
        return CanAuthenticate(account, [], out failure);
    }

    public static bool CanAuthenticate(AccountData account, IReadOnlyCollection<string> identityKeys,
        out LoginFailure failure)
    {
        failure = LoginFailure.InvalidCredentials();

        if (AccountBanHelper.TryGetActiveBan(account, identityKeys, out var banStatus))
        {
            account.ClearLoginTokens(persist: false);
            DatabaseHelper.QueueSaveDatabaseType(account);
            failure = LoginFailure.Banned(banStatus);
            return false;
        }

        if (account.CanLogin())
            return true;

        account.ClearLoginTokens(persist: false);
        DatabaseHelper.QueueSaveDatabaseType(account);
        failure = LoginFailure.EmailVerificationRequired();
        return false;
    }

    public static bool TryCreateSession(string? account, string? password, out LoginSession session, out string errorMessage)
    {
        LoginFailure failure;
        var succeeded = TryCreateSession(account, password, out session, out failure);
        errorMessage = failure.Message;
        return succeeded;
    }

    internal readonly record struct LoginFailure(int Retcode, string Message)
    {
        public static LoginFailure InvalidCredentials()
        {
            return new LoginFailure(-201, InvalidCredentialsMessage);
        }

        public static LoginFailure EmailVerificationRequired()
        {
            return new LoginFailure(-202, EmailVerificationRequiredMessage);
        }

        public static LoginFailure Banned(AccountBanStatus banStatus)
        {
            return new LoginFailure(-203, banStatus.FormatLoginMessage());
        }
    }

    internal readonly record struct LoginSession(
        string Uid,
        string Username,
        string Email,
        bool IsEmailVerified,
        string DispatchToken);
}
