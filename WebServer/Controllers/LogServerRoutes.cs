using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace March7thHoney.WebServer.Controllers;

public static class LogServerRoutes
{
    public static void MapLogServerRoutes(this IEndpointRouteBuilder app)
    {
        Func<IResult> upload = () => Results.Text("{\"code\":0}", "application/json");
        app.MapPost("/sdk/dataUpload", upload);
        app.MapPost("/crashdump/dataUpload", upload);
        app.MapPost("/apm/dataUpload", upload);

        app.MapPost("/common/h5log/log/batch",
            () => Results.Text("{\"retcode\":0,\"message\":\"success\",\"data\":null}", "application/json"));
    }
}
