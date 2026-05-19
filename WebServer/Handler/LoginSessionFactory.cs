using March7thHoney.Database.Account;
using March7thHoney.Util;

namespace March7thHoney.WebServer.Handler;

internal static class LoginSessionFactory
{
    public const string InvalidCredentialsMessage = "Incorrect account or password";

    public static bool TryCreateSession(string? account, string? password, out LoginSession session, out LoginFailure failure)
    {
        session = default;
        failure = LoginFailure.InvalidCredentials();

        var normalizedAccount = (account ?? string.Empty).Trim();
        var normalizedPassword = password ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedAccount) || string.IsNullOrWhiteSpace(normalizedPassword))
            return false;

        var accountData = AccountData.GetAccountByUserName(normalizedAccount);
        if (accountData == null)
        {
            if (!ConfigManager.Config.ServerOption.AutoCreateUser ||
                !ConfigManager.Config.ServerOption.Auth.EnableRegistration)
                return false;

            try
            {
                AccountHelper.CreateAccount(normalizedAccount, normalizedPassword, 0);
            }
            catch (Exception ex)
            {
                failure = new LoginFailure(-201, ex.Message);
                return false;
            }

            accountData = AccountData.GetAccountByUserName(normalizedAccount);
            if (accountData == null)
            {
                failure = new LoginFailure(-201, "Failed to create account");
                return false;
            }
        }

        var canonicalAccount = AccountData.GetAccountByUid(accountData.Uid) ?? accountData;
        if (!canonicalAccount.HasPassword())
        {
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

        var username = canonicalAccount.Username ?? normalizedAccount;
        canonicalAccount.SyncPermissionsWithRole();
        var dispatchToken = canonicalAccount.GenerateDispatchToken();
        session = new LoginSession(canonicalAccount.Uid.ToString(), username, username + "@StarRail.com",
            dispatchToken);
        return true;
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
    }

    internal readonly record struct LoginSession(string Uid, string Username, string Email, string DispatchToken);
}
