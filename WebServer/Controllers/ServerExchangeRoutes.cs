using CastoriceHoney.Server.Database;
using CastoriceHoney.Server.Database.Account;
using CastoriceHoney.Server.Util;
using Microsoft.AspNetCore.Mvc;

namespace CastoriceHoney.Server.WebServer.Controllers;

[ApiController]
[Route("/")]
public class ServerExchangeRoutes
{
    [HttpGet("/get_account_info")]
    public async ValueTask<ContentResult> GetAccountInfo([FromQuery] string accountUid)
    {
        if (!ConfigManager.Config.ServerOption.ServerConfig.RunDispatch)
            return new ContentResult
            {
                StatusCode = 404
            };

        if (string.IsNullOrEmpty(accountUid) || !int.TryParse(accountUid, out var uid))
            return new ContentResult
            {
                StatusCode = 400
            };

        var account = DatabaseHelper.Instance?.GetInstance<AccountData>(uid);
        if (account == null)
            return new ContentResult
            {
                StatusCode = 404,
                Content = "Account not found"
            };

        await ValueTask.CompletedTask;

        return new ContentResult
        {
            Content = account.Uid.ToString(),
            StatusCode = 200,
            ContentType = "plain/text; charset=utf-8"
        };
    }
}