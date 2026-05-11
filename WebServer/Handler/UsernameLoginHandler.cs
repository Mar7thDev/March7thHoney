using March7thHoney.Database.Account;
using March7thHoney.Util;
using March7thHoney.WebServer.Objects;
using Microsoft.AspNetCore.Mvc;
using static March7thHoney.WebServer.Objects.LoginResJson;

namespace March7thHoney.WebServer.Handler;

public class UsernameLoginHandler
{
    public JsonResult Handle(string account, string password, bool isCrypto)
    {
        LoginResJson res = new();
        var accountData = AccountData.GetAccountByUserName(account);

        if (accountData == null)
        {
            if (ConfigManager.Config.ServerOption.AutoCreateUser)
            {
                AccountHelper.CreateAccount(account, 0);
                accountData = AccountData.GetAccountByUserName(account);
            }
            else
            {
                return new JsonResult(new LoginResJson { message = "Account not found", retcode = -201 });
            }
        }

        if (accountData != null)
        {
            var canonicalAccount = AccountData.GetAccountByUid(accountData.Uid) ?? accountData;
            var username = canonicalAccount.Username ?? account;
            res.message = "OK";
            res.data = new VerifyData(canonicalAccount.Uid.ToString(), username + "@StarRail.com",
                canonicalAccount.GenerateDispatchToken());
            res.data.account.name = username;
            res.data.account.is_email_verify = "1";
        }

        return new JsonResult(res);
    }
}
