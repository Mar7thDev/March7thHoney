using March7thHoney.Util;
using March7thHoney.WebServer.Handler;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace March7thHoney.WebServer.Controllers;

public static partial class AdminRoutes
{
    private static void MapSettingsRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/api/settings", GetSettings);
        app.MapPut("/admin/api/settings", UpdateSettings);
    }

    private static IResult GetSettings(HttpContext ctx)
    {
        if (!IsAuthorized(ctx, out var unauthorized))
            return unauthorized;

        return Success(new SettingsData(AdminSettingsRegistry.Snapshot()));
    }

    private static IResult UpdateSettings(HttpContext ctx, AdminSettingsRequest? request)
    {
        if (!IsAuthorized(ctx, out var unauthorized))
            return unauthorized;

        request ??= new AdminSettingsRequest();
        if (request.values == null || request.values.Count == 0)
            return Error("Nothing to save");

        var result = AdminSettingsRegistry.Apply(request.values);
        return Success(new SettingsApplyData(
            result.Applied,
            result.Errors,
            result.RestartRequired,
            result.Persisted,
            AdminSettingsRegistry.Snapshot()));
    }

    public class AdminSettingsRequest
    {
        public Dictionary<string, string>? values { get; set; }
    }
}
