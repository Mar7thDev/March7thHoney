using March7thHoney.Database.Account;
using March7thHoney.Util;
using March7thHoney.WebServer.Objects;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using static March7thHoney.WebServer.Objects.NewLoginResJson;

namespace March7thHoney.WebServer.Handler;

public class NewUsernameLoginHandler
{
    public JsonResult Handle(string account, string password)
    {
        var session = ResolveSession(account);
        if (session == null)
            return new JsonResult(new NewLoginResJson { message = "Account not found", retcode = -201 });

        var res = new NewLoginResJson
        {
            message = "OK",
            data = new VerifyData(session.Uid, session.Email, session.DispatchToken)
        };
        res.data.user_info.account_name = session.Username;
        res.data.user_info.area_code = "**";
        res.data.user_info.country = "US";
        res.data.user_info.is_email_verify = "1";

        return new JsonResult(res);
    }

    public ContentResult HandleLegacy(string account, string password)
    {
        var session = ResolveSession(account);
        if (session == null)
        {
            return new ContentResult
            {
                ContentType = "application/json",
                Content = "{\"retcode\":-201,\"message\":\"Account not found\"}"
            };
        }

        var body = new
        {
            retcode = 0,
            message = "OK",
            data = new
            {
                user_info = new
                {
                    aid = session.Uid,
                    mid = session.Uid,
                    account_name = session.Username,
                    email = session.Email,
                    is_email_verify = 0,
                    area_code = "**",
                    mobile = "",
                    safe_area_code = "",
                    safe_mobile = "",
                    realname = "",
                    identity_code = "",
                    rebind_area_code = "",
                    rebind_mobile = "",
                    rebind_mobile_time = "1",
                    links = Array.Empty<object>(),
                    country = "CN",
                    password_time = "1",
                    is_adult = 0,
                    unmasked_email = "",
                    unmasked_email_type = 0
                },
                token = new { token_type = 1, token = session.DispatchToken },
                ext_user_info = new { guardian_email = "", birth = "0" }
            }
        };

        return new ContentResult
        {
            ContentType = "application/json",
            Content = JsonConvert.SerializeObject(body)
        };
    }

    private static LoginSession? ResolveSession(string account)
    {
        var accountData = AccountData.GetAccountByUserName(account);

        if (accountData == null)
        {
            if (!ConfigManager.Config.ServerOption.AutoCreateUser)
                return null;

            AccountHelper.CreateAccount(account, 0);
            accountData = AccountData.GetAccountByUserName(account);
            if (accountData == null)
                return null;
        }

        var canonicalAccount = AccountData.GetAccountByUid(accountData.Uid) ?? accountData;
        var username = canonicalAccount.Username ?? account;

        return new LoginSession(
            canonicalAccount.Uid.ToString(),
            username,
            username + "@StarRail.com",
            canonicalAccount.GenerateDispatchToken());
    }

    private sealed record LoginSession(string Uid, string Username, string Email, string DispatchToken);
}
