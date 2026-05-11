using March7thHoney.Database.Account;
using March7thHoney.Util;
using March7thHoney.WebServer.Objects;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using static March7thHoney.WebServer.Objects.NewLoginResJson;

namespace March7thHoney.WebServer.Handler;

public class NewTokenLoginHandler
{
    public ContentResult Handle(string mid, string token)
    {
        if (!int.TryParse(mid, out var uid))
        {
            return new ContentResult
            {
                ContentType = "application/json",
                Content = "{\"retcode\":-201,\"message\":\"Account not found\"}"
            };
        }

        var account = AccountData.GetAccountByUid(uid);
        if (account == null || !string.Equals(account.DispatchToken, token, StringComparison.Ordinal))
        {
            return new ContentResult()
            {
                ContentType = "application/json",
                Content = "{\"retcode\":-201,\"message\":\"Account not found\"}"
            };
        }

        var uidStr = account.Uid.ToString();
        var dispatchToken = token;
        var email = account.Username + "@StarRail.com";
        var rsp =
            $"{{\"retcode\":0,\"message\":\"OK\",\"data\":{{\"user_info\":{{\"aid\":\"{uidStr}\",\"mid\":\"{uidStr}\",\"account_name\":\"{account.Username}\",\"email\":\"{email}\",\"is_email_verify\":0,\"area_code\":\"**\",\"mobile\":\"\",\"safe_area_code\":\"\",\"safe_mobile\":\"\",\"realname\":\"\",\"identity_code\":\"\",\"rebind_area_code\":\"\",\"rebind_mobile\":\"\",\"rebind_mobile_time\":\"1\",\"links\":[],\"country\":\"CN\",\"password_time\":\"1\",\"is_adult\":0,\"unmasked_email\":\"\",\"unmasked_email_type\":0}},\"token\":{{\"token_type\":1,\"token\":\"{dispatchToken}\"}},\"ext_user_info\":{{\"guardian_email\":\"\",\"birth\":\"0\"}}}}}}";

        return new ContentResult()
        {
            ContentType = "application/json",
            Content = rsp,
        };
    }
}
