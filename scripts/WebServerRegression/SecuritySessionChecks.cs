using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using March7thHoney.Database.Account;
using March7thHoney.Util;
using March7thHoney.WebServer.Handler;
using March7thHoney.WebServer.Objects;
using March7thHoney.WebServer.Server;

internal static class SecuritySessionChecks
{
    // Uses only the regression runner's scratch database/config and loopback HTTP server.
    public static async Task Run(HttpClient client, int fixtureUid, string fixturePassword,
        Action<string, bool> check)
    {
        var config = ConfigManager.Config;
        var rsa = config.WebSecurity.EnableRsaLoginDecrypt;
        try
        {
            foreach (var enabled in new[] { false, true })
            {
                config.WebSecurity.EnableRsaLoginDecrypt = enabled;
                foreach (var password in new[] { "", "not-the-password" })
                {
                    using var response = await client.PostAsJsonAsync("/sr-tools",
                        new { username = fixtureUid.ToString(), password });
                    check($"sr-tools requires password with RSA={enabled} ({(password.Length == 0 ? "empty" : "wrong")})",
                        response.StatusCode == HttpStatusCode.Unauthorized);
                }
                // No data is submitted, and the fixture is offline: valid authentication reaches the
                // expected offline guard, not the import code or a misleading password failure.
                using var valid = await client.PostAsJsonAsync("/sr-tools",
                    new { username = fixtureUid.ToString(), password = fixturePassword });
                check($"sr-tools accepts valid credentials independently of RSA={enabled}",
                    valid.StatusCode == HttpStatusCode.BadRequest &&
                    (await valid.Content.ReadAsStringAsync()).Contains("player offline"));
            }
        }
        finally { config.WebSecurity.EnableRsaLoginDecrypt = rsa; }

        const int uid = 10003;
        AccountHelper.CreateAccount("session-security", "session-security@example.invalid", "fixture-initial-only", uid, null);
        var account = AccountData.GetAccountByUid(uid)!;
        var old = PlayerConsoleSessionManager.Create(account);
        check("new player session resolves", PlayerConsoleSessionManager.TryResolve(old.Token, out _, out _));
        AccountHelper.SetPassword(account, "fixture-changed-only");
        check("password changes invalidate old player console token",
            !PlayerConsoleSessionManager.TryResolve(old.Token, out _, out _));
        var resetSession = PlayerConsoleSessionManager.Create(account);
        account.MarkEmailVerified();
        var resetToken = account.GeneratePasswordResetToken();
        new AccountRecoveryHandler().ResetPassword(new ResetPasswordReqJson
        {
            uid = uid.ToString(), token = resetToken, password = "fixture-reset-only", confirm_password = "fixture-reset-only"
        });
        check("password recovery completed", account.VerifyPassword("fixture-reset-only"));
        check("password recovery invalidates old player console token",
            !PlayerConsoleSessionManager.TryResolve(resetSession.Token, out _, out _));
        var current = PlayerConsoleSessionManager.Create(account);
        check("fresh session works after recovery", PlayerConsoleSessionManager.TryResolve(current.Token, out _, out _));
        PlayerConsoleSessionManager.RevokeForUid(uid);

        var originalKey = config.MuipServer.AdminKey;
        MuipManager.Sessions.Clear();
        try
        {
            var created = MuipManager.CreateSession("XML");
            var id = created.Data!.SessionId;
            check("MUIP new session authenticates", MuipManager.AuthAdmin(id, originalKey).Code == 0);
            var session = MuipManager.GetSession(id)!;
            config.MuipServer.AdminKey = "regression-rotated-only";
            check("MUIP lookup rejects old key generation", MuipManager.GetSession(id) == null);
            MuipManager.Sessions[id] = session;
            check("MUIP authentication rejects session created before key rotation",
                MuipManager.AuthAdmin(id, config.MuipServer.AdminKey).Code != 0);
            MuipManager.Sessions[id] = session;
            check("MUIP command rejects old key generation",
                (await MuipManager.ExecuteCommandAsync(id, "/help", 0)).Code != 0);
            MuipManager.Sessions[id] = session;
            check("MUIP server info rejects old key generation", MuipManager.GetInformation(id).Code != 0);
            MuipManager.Sessions[id] = session;
            check("MUIP player info rejects old key generation", MuipManager.GetPlayerInformation(id, fixtureUid).Code != 0);
            MuipManager.Sessions.Clear();
            config.MuipServer.AdminKey = originalKey;

            var rotationId = MuipManager.CreateSession("XML").Data!.SessionId;
            MuipManager.AuthAdmin(rotationId, originalKey);
            using (var rotated = await client.PostAsJsonAsync("/admin/api/maintenance/rotate-admin-key", new { }))
                check("admin rotation endpoint succeeds", rotated.IsSuccessStatusCode);
            check("admin rotation removes MUIP sessions", !MuipManager.Sessions.ContainsKey(rotationId));
            config.MuipServer.AdminKey = originalKey;

            MuipManager.Sessions.Clear();
            var clientRejected = 0;
            for (var i = 0; i < 40; i++)
            {
                using var response = await client.PostAsJsonAsync("/muip/create_session", new { key_type = "XML" });
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (json.RootElement.GetProperty("code").GetInt32() != 0) clientRejected++;
            }
            check("MUIP HTTP session admission bounds each client", clientRejected > 0 && MuipManager.Sessions.Count <= 32);
            MuipManager.Sessions.Clear();

            Parallel.For(0, 128, _ => MuipManager.CreateSession("XML", "concurrent-regression-client"));
            check("MUIP concurrent admission preserves per-client capacity", MuipManager.Sessions.Count == 32);
            MuipManager.Sessions.Clear();

            var rejected = 0;
            for (var i = 0; i < 1100; i++)
                if (MuipManager.CreateSession("XML").Code != 0) rejected++;
            check("anonymous MUIP session storage is bounded", rejected > 0 && MuipManager.Sessions.Count <= 1024);
            foreach (var pending in MuipManager.Sessions.Values)
                pending.ExpireTimeStamp = DateTime.Now.AddMinutes(-1).ToUnixSec();
            var afterExpiry = MuipManager.CreateSession("XML");
            check("MUIP admission cleans abandoned expired sessions", afterExpiry.Code == 0 && MuipManager.Sessions.Count == 1);

            foreach (var pending in MuipManager.Sessions.Values)
                pending.ExpireTimeStamp = DateTime.Now.AddMinutes(-1).ToUnixSec();
            // Trigger the actual background timer without a minute-long test delay; no session API
            // is invoked while waiting, so lazy cleanup cannot accidentally satisfy this assertion.
            var timer = typeof(MuipManager).GetField("SessionCleanupTimer", BindingFlags.NonPublic | BindingFlags.Static)
                ?.GetValue(null) as Timer;
            check("MUIP has proactive cleanup timer", timer != null);
            if (timer != null)
            {
                try
                {
                    timer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);
                    for (var i = 0; i < 100 && MuipManager.Sessions.Count > 0; i++) await Task.Delay(20);
                    check("MUIP timer removes abandoned sessions without requests", MuipManager.Sessions.Count == 0);
                }
                finally { timer.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1)); }
            }

            var enabledSessionId = MuipManager.CreateSession("XML").Data!.SessionId;
            MuipManager.AuthAdmin(enabledSessionId, originalKey);
            config.MuipServer.AdminKey = "";
            check("disabling MUIP revokes existing administrator sessions", MuipManager.GetSession(enabledSessionId) == null);
            check("disabled MUIP rejects anonymous creation", MuipManager.CreateSession("XML").Code != 0);
            var playerOnly = new MuipSession
            {
                SessionId = "disabled-muip-player", IsAdmin = false, IsAuthorized = true,
                ExpireTimeStamp = DateTime.Now.AddMinutes(1).ToUnixSec()
            };
            MuipManager.Sessions[playerOnly.SessionId] = playerOnly;
            check("player console bridge survives disabled MUIP", MuipManager.GetSession(playerOnly.SessionId) != null);
        }
        finally
        {
            config.MuipServer.AdminKey = originalKey;
            MuipManager.Sessions.Clear();
        }
    }
}
