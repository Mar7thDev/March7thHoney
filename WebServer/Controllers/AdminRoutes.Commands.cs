using System.Text;
using March7thHoney.Util;
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

        var sessionId = $"admin-page-{Guid.NewGuid():N}";
        MuipManager.Sessions[sessionId] = new MuipSession
        {
            SessionId = sessionId,
            ExpireTimeStamp = DateTime.Now.AddMinutes(1).ToUnixSec(),
            IsAdmin = true,
            IsAuthorized = true
        };

        try
        {
            // The MUIP layer accepts RSA or Base64 command payloads. Passing admin-page plaintext through
            // directly is ambiguous for commands made only of Base64 characters (for example "/windy loaded"),
            // so encode it explicitly just like the player console does.
            var encodedCommand = Convert.ToBase64String(Encoding.UTF8.GetBytes(command));
            var response = await MuipManager.ExecuteCommandAsync(sessionId, encodedCommand,
                request.target_uid.GetValueOrDefault());
            if (response.Code != 0)
                return Error(response.Message);

            var output = string.Empty;
            if (!string.IsNullOrWhiteSpace(response.Data?.Message))
                output = Encoding.UTF8.GetString(Convert.FromBase64String(response.Data.Message));

            return Success(new ExecCommandData(output.TrimEnd(), request.target_uid.GetValueOrDefault()));
        }
        catch (Exception ex)
        {
            return Error(ex.Message);
        }
        finally
        {
            MuipManager.Sessions.TryRemove(sessionId, out _);
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
