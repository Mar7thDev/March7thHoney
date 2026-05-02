using CastoriceHoney.Server.Util;
using CastoriceHoney.Server.WebServer.Handler;
using Microsoft.AspNetCore.Mvc;
using CastoriceHoney.Server.WebServer.Request;
namespace CastoriceHoney.Server.WebServer.Controllers;

[ApiController]
[Route("/")]
public class GateServerRoutes
{
    [HttpGet("/query_gateway")]
    public async ValueTask<ContentResult> QueryGateway([FromQuery] GateWayRequest req)
    {
        if (!ConfigManager.Config.ServerOption.ServerConfig.RunGateway)
            return new ContentResult
            {
                StatusCode = 404
            };

        await ValueTask.CompletedTask;
        var handler = new QueryGatewayHandler(req);
        return new ContentResult
        {
            Content = handler.Data,
            StatusCode = 200,
            ContentType = "text/plain"
        };
    }
}
