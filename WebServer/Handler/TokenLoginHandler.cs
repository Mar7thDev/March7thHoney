using March7thHoney.Database.Account;
using March7thHoney.WebServer.Objects;
using Microsoft.AspNetCore.Mvc;
using static March7thHoney.WebServer.Objects.LoginResJson;

namespace March7thHoney.WebServer.Handler;

public class TokenLoginHandler
{
    public JsonResult Handle(string uid, string token)
    {
        var account = AccountData.GetAccountByUid(int.Parse(uid));
        var res = new LoginResJson();
        if (account == null || !account?.DispatchToken?.Equals(token) == true)
        {
            res.retcode = -201;
            res.message = "Game account cache information error";
        }
        else
        {
            res.message = "OK";
            res.data = new VerifyData(account!.Uid.ToString(), account.Username + "@StarRail.com", token);
            res.data.account.name = account.Username;
            res.data.account.is_email_verify = "1";
        }

        return new JsonResult(res);
    }
}
