using March7thHoney.Util;
using March7thHoney.WebServer.Handler;
using March7thHoney.WebServer.Request;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace March7thHoney.WebServer.Controllers;

public static class GateServerRoutes
{
    public static void MapGateServerRoutes(this IEndpointRouteBuilder app)
    {
        app.MapGet("/query_gateway", ([AsParameters] GateWayRequest req) =>
        {
            if (!ConfigManager.Config.ServerOption.ServerConfig.RunGateway)
                return Results.StatusCode(404);

            var handler = new QueryGatewayHandler(req);
            return Results.Text(handler.Data, "text/plain");
        });
    }
}
