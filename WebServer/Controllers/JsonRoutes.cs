using March7thHoney.Database.Account;
using March7thHoney.GameServer.Game.Calyx;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server;
using March7thHoney.Internationalization;
using March7thHoney.Kcp;
using March7thHoney.Util;
using March7thHoney.WebServer.Server;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace March7thHoney.WebServer.Controllers
{
    [ApiController]
    [Route("/")]
    public class JsonRoutes : ControllerBase
    {
        private static readonly Logger logger = Logger.GetByClassName();

        [HttpPost("/sr-tools")]
        public async ValueTask<ContentResult> SrTools()
        {
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                string body = await reader.ReadToEndAsync();

                var obj = Newtonsoft.Json.Linq.JToken.Parse(body);
                string username = obj["username"]?.ToString() ?? "";
                string password = obj["password"]?.ToString() ?? "";
                string dataJson = obj["data"]?.ToString(Newtonsoft.Json.Formatting.None) ?? "";

                if (!int.TryParse(username, out var uid))
                {
                    return new ContentResult()
                    {
                        StatusCode = 400,
                        Content = "Username field must be the player's UID (integer).",
                        ContentType = "text/plain"
                    };
                }

                var acc = AccountData.GetAccountByUid(uid);

                if (acc == null)
                {
                    return new ContentResult()
                    {
                        StatusCode = 400,
                        Content = "uid not found on server",
                        ContentType = "text/plain"
                    };
                }

                if (ConfigManager.Config.WebSecurity.EnableRsaLoginDecrypt &&
                    (string.IsNullOrEmpty(password) || !acc.VerifyPassword(password)))
                {
                    return new ContentResult()
                    {
                        StatusCode = 401,
                        Content = "Invalid password for this uid.",
                        ContentType = "text/plain"
                    };
                }

                Connection? con = null;

                foreach (var item in March7thHoneyListener.Connections.Values)
                {
                    if (item is not Connection c)
                    {
                        continue;
                    }

                    if (c.Player?.Uid == acc.Uid)
                    {
                        con = c;
                        break;
                    }
                }

                if (con?.Player == null)
                {
                    return new ContentResult()
                    {
                        StatusCode = 400,
                        Content = "player offline",
                        ContentType = "text/plain"
                    };
                }

                if (dataJson != string.Empty)
                {
                    var player = con.Player;
                    await FreesrShared.ImportJson(dataJson, player, async (msg) =>
                    {
                        if (msg[0] == "Game.Command.Json.ImportSummary" || msg[0] == "Game.Command.Json.AvatarExcelNotFound")
                        {
                            return;
                        }
                        else
                        {
                            throw new Exception(I18NManager.Translate(msg[0], msg.Skip(1).ToArray()));
                        }
                    });
                }

                return new ContentResult()
                {
                    StatusCode = 200,
                    Content = "Successfully synced data from website!",
                    ContentType = "application/json"
                };
            }
            catch (Exception ex)
            {
                return new ContentResult()
                {
                    StatusCode = 500,
                    Content = ex.Message,
                    ContentType = "text/plain"
                };
            }
        }

        // Launcher console entry: authenticate with UID + password (same as
        // sr-tools) and run a command with the EXACT permissions of in-game chat.
        // An empty command performs a credential check only (used by "Connect").
        [HttpPost("/console/exec")]
        public async ValueTask<ContentResult> ConsoleExec()
        {
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                string body = await reader.ReadToEndAsync();

                var obj = Newtonsoft.Json.Linq.JToken.Parse(body);
                string username = obj["username"]?.ToString() ?? "";
                string password = obj["password"]?.ToString() ?? "";
                string command = obj["command"]?.ToString() ?? "";

                if (!int.TryParse(username, out var uid))
                    return ConsoleJson(1, "Username field must be the player's UID (integer).", null, 400);

                var acc = AccountData.GetAccountByUid(uid);
                if (acc == null)
                    return ConsoleJson(1, "uid not found on server", null, 400);

                // Always verify the password when the account has one set, regardless of
                // the RSA-login flag — command execution must not be password-bypassable.
                if (acc.HasPassword() && (string.IsNullOrEmpty(password) || !acc.VerifyPassword(password)))
                    return ConsoleJson(1, "Invalid password for this uid.", null, 401);

                // Commands run against the live session, so the player must be online.
                // Use the same active-connection lookup the command pipeline uses.
                var con = Listener.GetActiveConnection(acc.Uid);
                if (con?.Player == null)
                    return ConsoleJson(1, "player offline", null, 400);

                command = command.Trim();
                if (command.Length > 4096)
                    return ConsoleJson(1, "Command too long.", null, 400);

                if (string.IsNullOrWhiteSpace(command))
                    return ConsoleJson(0, "OK", $"Connected as UID {acc.Uid} (role: {acc.GetRole()}).");

                // Run through the MUIP pipeline with a non-admin session bound to the
                // player's account and targeting their own uid. MuipCommandSender then
                // resolves permissions via Account.HasPermission, identical to chat.
                var sessionId = $"console-{Guid.NewGuid():N}";
                MuipManager.Sessions[sessionId] = new MuipSession
                {
                    SessionId = sessionId,
                    ExpireTimeStamp = DateTime.Now.AddMinutes(1).ToUnixSec(),
                    IsAdmin = false,
                    IsAuthorized = true,
                    Account = acc
                };

                try
                {
                    var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(command));
                    var response = MuipManager.ExecuteCommand(sessionId, encoded, acc.Uid);
                    if (response.Code != 0)
                        return ConsoleJson(1, response.Message, null, 400);

                    var output = string.Empty;
                    if (!string.IsNullOrWhiteSpace(response.Data?.Message))
                        output = Encoding.UTF8.GetString(Convert.FromBase64String(response.Data.Message));

                    return ConsoleJson(0, "OK", output.TrimEnd());
                }
                finally
                {
                    MuipManager.Sessions.Remove(sessionId);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Console command execution failed", ex);
                return ConsoleJson(1, "Internal server error.", null, 500);
            }
        }

        // Launcher self-reset: authenticate with UID + password and reset ONLY that
        // player's own gameplay data (self-only by construction). Works offline, so a
        // crash-locked account can be reset before logging back in.
        [HttpPost("/console/self-reset")]
        public async ValueTask<ContentResult> ConsoleSelfReset()
        {
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                string body = await reader.ReadToEndAsync();

                var obj = Newtonsoft.Json.Linq.JToken.Parse(body);
                string username = obj["username"]?.ToString() ?? "";
                string password = obj["password"]?.ToString() ?? "";
                var confirmed = string.Equals(obj["confirm"]?.ToString(), "true", StringComparison.OrdinalIgnoreCase);

                if (!int.TryParse(username, out var uid))
                    return ConsoleJson(1, "Username field must be the player's UID (integer).", null, 400);

                var acc = AccountData.GetAccountByUid(uid);
                if (acc == null)
                    return ConsoleJson(1, "uid not found on server", null, 400);

                if (acc.HasPassword() && (string.IsNullOrEmpty(password) || !acc.VerifyPassword(password)))
                    return ConsoleJson(1, "Invalid password for this uid.", null, 401);

                if (!confirmed)
                    return ConsoleJson(1, "Reset requires confirmation.", null, 400);

                await PlayerResetHelper.ResetGameplayAsync(acc.Uid);

                return ConsoleJson(0, "OK", $"Your save data has been reset (UID {acc.Uid}). Please log in again.");
            }
            catch (Exception ex)
            {
                logger.Error("Console self-reset failed", ex);
                return ConsoleJson(1, "Internal server error.", null, 500);
            }
        }

        private static ContentResult ConsoleJson(int retcode, string message, string? output, int statusCode = 200)
        {
            var json = JsonSerializer.Serialize(new
            {
                retcode,
                message,
                output = output ?? string.Empty
            });
            return new ContentResult
            {
                StatusCode = statusCode,
                Content = json,
                ContentType = "application/json"
            };
        }

        [HttpGet("/sr-tools-export")]
        public async ValueTask<EmptyResult> DownloadFreedata()
        {
            int uid = 0;
            if (Request.Query.ContainsKey("uid"))
            {
                int.TryParse(Request.Query["uid"], out uid);
            }

            if (uid == 0)
            {
                Response.StatusCode = 400;
                Response.ContentType = "text/plain";
                await Response.WriteAsync("Error: Missing valid uid parameter");
                return new EmptyResult();
            }

            var json = await FreesrShared.ExportPlayerDataAsync(uid, true);

            Response.StatusCode = 200;
            Response.ContentType = "application/octet-stream";
            Response.Headers["Content-Disposition"] = "attachment; filename=freesr-data.json; filename*=UTF-8''freesr-data.json";

            await Response.WriteAsync(json);
            return new EmptyResult();
        }
    }
}
