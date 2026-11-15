using System.Text;
using System.Text.Json.Nodes;
using March7thHoney.Database.Account;
using March7thHoney.GameServer.Game.Calyx;
using March7thHoney.GameServer.Server;
using March7thHoney.Internationalization;
using March7thHoney.Kcp;
using March7thHoney.Util;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace March7thHoney.WebServer.Controllers;

public static class JsonRoutes
{
    public static void MapJsonRoutes(this IEndpointRouteBuilder app)
    {
        app.MapPost("/sr-tools", SrTools);
        app.MapGet("/sr-tools-export", DownloadFreedata);
    }

    private static async Task<IResult> SrTools(HttpRequest request)
    {
        try
        {
            using var reader = new StreamReader(request.Body, Encoding.UTF8);
            string body = await reader.ReadToEndAsync();

            var obj = JsonNode.Parse(body);
            string username = obj?["username"]?.ToString() ?? "";
            string password = obj?["password"]?.ToString() ?? "";
            string dataJson = obj?["data"]?.ToJsonString() ?? "";

            if (!int.TryParse(username, out var uid))
                return Results.Text("Username field must be the player's UID (integer).", "text/plain", null, 400);

            var acc = AccountData.GetAccountByUid(uid);
            if (acc == null)
                return Results.Text("uid not found on server", "text/plain", null, 400);

            // Save import always requires credentials. RSA only controls login transport decryption,
            // not permission to overwrite a save. Accounts without a password must set one first.
            if (string.IsNullOrEmpty(password) || !acc.VerifyPassword(password))
                return Results.Text("Invalid password for this uid.", "text/plain", null, 401);

            Connection? con = null;
            foreach (var item in March7thHoneyListener.GetSnapshot())
            {
                if (item is not Connection c) continue;
                if (c.Player?.Uid == acc.Uid)
                {
                    con = c;
                    break;
                }
            }

            if (con?.Player == null)
                return Results.Text("player offline", "text/plain", null, 400);

            if (dataJson != string.Empty)
            {
                var player = con.Player;
                await FreesrShared.ImportJson(dataJson, player, async msg =>
                {
                    if (msg[0] == "Game.Command.Json.ImportSummary" || msg[0] == "Game.Command.Json.AvatarExcelNotFound")
                        return;
                    throw new Exception(I18NManager.Translate(msg[0], msg.Skip(1).ToArray()));
                });
            }

            return Results.Text("Successfully synced data from website!", "application/json");
        }
        catch (Exception ex)
        {
            return Results.Text(ex.Message, "text/plain", null, 500);
        }
    }

    private static async Task<IResult> DownloadFreedata(HttpRequest request, HttpResponse response)
    {
        int uid = 0;
        if (request.Query.ContainsKey("uid"))
            int.TryParse(request.Query["uid"], out uid);

        if (uid == 0)
            return Results.Text("Error: Missing valid uid parameter", "text/plain", null, 400);

        var acc = AccountData.GetAccountByUid(uid);
        if (acc == null)
            return Results.Text("uid not found on server", "text/plain", null, 400);

        // Always verify the password when the account has one set, regardless of the RSA-login
        // flag (same rule as the player console) — this endpoint exports a player's full save data
        // and must not be reachable by uid alone.
        var password = request.Query.TryGetValue("password", out var pw) ? pw.ToString() : "";
        if (acc.HasPassword() && (string.IsNullOrEmpty(password) || !acc.VerifyPassword(password)))
            return Results.Text("Invalid password for this uid.", "text/plain", null, 401);

        var json = await FreesrShared.ExportPlayerDataAsync(uid, true);

        response.Headers["Content-Disposition"] =
            "attachment; filename=freesr-data.json; filename*=UTF-8''freesr-data.json";
        return Results.Text(json, "application/octet-stream");
    }
}
