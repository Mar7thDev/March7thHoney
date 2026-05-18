using March7thHoney.Database.Account;
using March7thHoney.WebServer.Objects;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace March7thHoney.WebServer.Handler;

public class TokenLoginHandler
{
    public JsonResult HandleShieldVerify(string uid, string token)
    {
        if (!int.TryParse(uid, out var parsedUid))
            return new JsonResult(LoginResJson.Error(-201, "Game account cache information error"));

        var account = AccountData.GetAccountByUid(parsedUid);
        if (account == null || !account.ValidateDispatchToken(token))
            return new JsonResult(LoginResJson.Error(-201, "Game account cache information error"));

        var username = account.Username ?? uid;
        return new JsonResult(LoginResJson.Success(
            account.Uid.ToString(),
            username,
            username + "@StarRail.com",
            token));
    }

    public ContentResult HandlePassportVerify(string mid, string token, bool refresh)
    {
        if (!int.TryParse(mid, out var uid))
            return BuildJson(PassportLoginResJson.Error(-201, "Account not found"));

        var account = AccountData.GetAccountByUid(uid);
        if (account == null)
            return BuildJson(PassportLoginResJson.Error(-201, "Account not found"));

        if (!account.ValidateDispatchToken(token))
            return BuildJson(PassportLoginResJson.Error(-201, "Game account cache information error"));

        var dispatchToken = refresh ? account.GenerateDispatchToken() : account.DispatchToken!;
        return BuildJson(PassportLoginResJson.Success(
            account.Uid.ToString(),
            account.Username ?? uid.ToString(),
            (account.Username ?? uid.ToString()) + "@StarRail.com",
            dispatchToken));
    }

    private static ContentResult BuildJson(PassportLoginResJson payload)
    {
        return new ContentResult
        {
            ContentType = "application/json",
            Content = JsonConvert.SerializeObject(payload)
        };
    }
}
