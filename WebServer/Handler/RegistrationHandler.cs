using March7thHoney.Database.Account;
using March7thHoney.Util;
using March7thHoney.WebServer.Objects;
using Microsoft.AspNetCore.Mvc;

namespace March7thHoney.WebServer.Handler;

public class RegistrationHandler
{
    public JsonResult Handle(RegisterReqJson req)
    {
        if (!ConfigManager.Config.ServerOption.Auth.EnableRegistration)
            return Error("Registration is disabled");

        var username = ResolveUsername(req);
        var password = req.password ?? string.Empty;
        var confirmation = req.confirm_password ?? req.confirmPassword ?? req.confirm;

        if (string.IsNullOrWhiteSpace(username))
            return Error("Username cannot be empty");

        if (!string.IsNullOrEmpty(confirmation) && !string.Equals(password, confirmation, StringComparison.Ordinal))
            return Error("Passwords do not match");

        if (AccountData.GetAccountByUserName(username) != null)
            return Error("Username already exists");

        try
        {
            AccountHelper.CreateAccount(username, password, 0);
        }
        catch (Exception ex)
        {
            return Error(ex.Message);
        }

        LoginSessionFactory.LoginFailure failure;
        if (!LoginSessionFactory.TryCreateSession(username, password, out var session, out failure))
            return Error(failure.Message, failure.Retcode);

        return new JsonResult(new
        {
            retcode = 0,
            message = "OK",
            data = new
            {
                account = new
                {
                    uid = session.Uid,
                    name = session.Username,
                    email = session.Email,
                    token = session.DispatchToken,
                    is_email_verify = "1"
                },
                user_info = new
                {
                    aid = session.Uid,
                    mid = session.Uid,
                    account_name = session.Username,
                    email = session.Email,
                    is_email_verify = "1"
                },
                token = new { token_type = 1, token = session.DispatchToken }
            }
        });
    }

    private static JsonResult Error(string message, int retcode = -201)
    {
        return new JsonResult(new { retcode, message, data = (object?)null });
    }

    private static string ResolveUsername(RegisterReqJson req)
    {
        return (req.account ?? req.username ?? req.email ?? string.Empty).Trim();
    }
}
