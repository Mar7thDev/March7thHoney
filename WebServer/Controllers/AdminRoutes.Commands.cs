using March7thHoney.WebServer.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace March7thHoney.WebServer.Controllers;

public static partial class AdminRoutes
{
    private static void MapCommandRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/api/commands", ExecuteCommand);
        app.MapGet("/admin/api/commands/catalog", GetCommandCatalog);
    }

    private static async Task<IResult> ExecuteCommand(HttpContext ctx, AdminExecuteCommandRequest? request)
    {
        if (!IsAuthorized(ctx, out var unauthorized))
            return unauthorized;

        request ??= new AdminExecuteCommandRequest();
        var command = (request.command ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(command))
            return Error("Command cannot be empty");
        if (command.Length > 4096)
            return Error("Command is too long");

        var targetUid = request.target_uid.GetValueOrDefault();
        try
        {
            // An administrator session with no implicit player: the target is only what the request names.
            var result = await MuipManager.RunCommandAsync(
                MuipManager.CreateLocalSession("admin-page", isAdmin: true), command, targetUid);
            if (result.Code != 0)
                return Error(result.Message);

            return Success(new ExecCommandData(result.Output.TrimEnd(), targetUid));
        }
        catch (Exception ex)
        {
            return Error(ex.Message);
        }
    }

    private static IResult GetCommandCatalog(HttpContext ctx)
    {
        if (!IsAuthorized(ctx, out var unauthorized))
            return unauthorized;

        var entries = MuipManager.CommandCatalogProvider?.Invoke() ?? [];
        var commands = entries
            .Select(entry => new CommandCatalogItem(
                entry.Name,
                entry.Description,
                entry.Usage,
                entry.Permission,
                entry.Aliases.ToList(),
                entry.Subcommands.ToList()))
            .OrderBy(entry => entry.name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Success(new CommandCatalogData(commands, commands.Count));
    }

    public class AdminExecuteCommandRequest
    {
        public string? command { get; set; }
        public int? target_uid { get; set; }
    }
}
