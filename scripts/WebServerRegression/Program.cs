using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using March7thHoney.Command;
using March7thHoney.Command.Command;
using March7thHoney.Database;
using March7thHoney.Database.Player;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server;
using March7thHoney.Internationalization;
using March7thHoney.Kcp;
using March7thHoney.Util;
using March7thHoney.WebServer;
using March7thHoney.WebServer.Server;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// An isolated HTTP/command regression runner. Never load the operator's Config.json or database.
var repository = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
var scratch = Path.Combine(Path.GetTempPath(), "m7h-web-regression-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(scratch);
Environment.CurrentDirectory = scratch;
var config = ConfigManager.Config;
config.Path.ConfigPath = Path.Combine(repository, "Config");
config.Path.LanguagePath = Path.Combine(repository, "Config/Languages");
config.Path.DatabasePath = Path.Combine(scratch, "Database");
config.Path.LogPath = Path.Combine(scratch, "Logs");
config.GameServer.UsePacketEncryption = false;
config.GameServer.GameServerName = "WebServer regression";
config.ServerOption.Language = "CHS";
config.HttpServer.UseSSL = false;
config.MuipServer.AdminKey = "local-web-regression-only";
_ = new DatabaseHelper();
I18NManager.LoadLanguage();
var manager = new CommandManager();
manager.RegisterCommands();
MuipManager.OnExecuteCommand += manager.HandleCommandAsync;
MuipManager.CommandCatalogProvider = manager.BuildCatalog;

var serve = args.Contains("--serve");
await using var app = WebProgram.BuildWebApp([], 0, serve ? "http://127.0.0.1:21000" : "http://127.0.0.1:0");
await app.StartAsync();
var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
if (serve)
{
    Console.WriteLine($"UI fixture: {address}/admin/index.html (key: local-web-regression-only)");
    await app.WaitForShutdownAsync();
    return;
}

var failures = new List<string>();
void Check(string name, bool pass)
{
    Console.WriteLine($"{(pass ? "PASS" : "FAIL")} {name}");
    if (!pass) failures.Add(name);
}

using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(60) };
using (var unauthorized = await client.GetAsync("/admin/api/maintenance"))
    Check("maintenance requires authentication", unauthorized.StatusCode == HttpStatusCode.Unauthorized);
using (var unauthorized = await client.PostAsJsonAsync("/admin/api/commands", new { command = "/help" }))
    Check("commands require authentication", unauthorized.StatusCode == HttpStatusCode.Unauthorized);
using (var response = await client.PostAsJsonAsync("/admin/api/login", new { admin_key = config.MuipServer.AdminKey }))
{
    response.EnsureSuccessStatusCode();
    using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.RootElement.GetProperty("data").GetProperty("token").GetString());
}

async Task<string> Execute(string command, int? target = null)
{
    using var response = await client.PostAsJsonAsync("/admin/api/commands", new { command, target_uid = target });
    response.EnsureSuccessStatusCode();
    using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    return json.RootElement.GetProperty("data").GetProperty("output").GetString()!.Replace("\r\n", "\n");
}

Check("help without a target", (await Execute("/help")).Contains("/help"));
Check("mail usage without a target", await Execute("/mail") == I18NManager.Translate("Game.Command.Mail.Usage").TrimEnd());
Check("player command still requires a target", await Execute("/give 1") == I18NManager.Translate("Game.Command.Notice.PlayerNotFound").TrimEnd());
Check("unknown command reports command error", await Execute("/no-such-command") == I18NManager.Translate("Game.Command.Notice.CommandNotFound").TrimEnd());

// Install a probe into the actual dispatcher so target and async behavior can be observed without gameplay writes.
var probeInfo = new CommandInfoAttribute("regression-probe", "probe", "probe");
var probe = new ProbeCommand();
var generated = (Dictionary<string, GeneratedCommand>)typeof(CommandManager)
    .GetField("_generated", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager)!;
manager.Commands[probeInfo.Name] = probe;
manager.CommandInfo[probeInfo.Name] = probeInfo;
generated[probeInfo.Name] = new GeneratedCommand(probeInfo, probe, [],
    new GeneratedCommandMethod("Probe", [], (_, arg) => probe.Run(arg)));

var connections = (SortedList<long, March7thHoneyConnection>)typeof(March7thHoneyListener)
    .GetField("Connections", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
var terminalTarget = new TestConnection(10001);
var webTarget = new TestConnection(10002);
connections.Add(1, terminalTarget);
connections.Add(2, webTarget);
manager.Target = terminalTarget;
Check("web console does not inherit terminal target", await Execute("/regression-probe") == "target=0;explicit=False");
Check("target field is explicit", await Execute("/regression-probe", 10002) == "target=10002;explicit=True");
Check("leading @uid selects only this command", await Execute("@10002 /regression-probe") == "target=10002;explicit=True");
Check("trailing @uid selects only this command", await Execute("/regression-probe @10002") == "target=10002;explicit=True");
await Execute("@10002");
Check("web target selection does not change terminal target", manager.Target == terminalTarget);
Check("web target selection does not leak to next request", await Execute("/regression-probe") == "target=0;explicit=False");
Check("invalid target is rejected", (await Execute("/help", 99999)).Contains("99999"));

// A real terminal retains its target behavior, observed through the probe's captured argument.
manager.HandleCommand("/regression-probe", new ConsoleCommandSender(new Logger("Regression")));
Check("terminal retains its selected target", probe.LastTarget == terminalTarget && probe.LastExplicit);
manager.Target = null;
connections.Clear();
Check("async command output is awaited", await Execute("/regression-probe delayed") == "before\nafter");
Check("async command failures reach the response", (await Execute("/regression-probe throws")).Contains(I18NManager.Translate("Game.Command.Notice.InternalError")));
var concurrentOutputs = await Task.WhenAll(Enumerable.Range(1, 12).Select(async id =>
    (Id: id, Output: await Execute($"/regression-probe delayed {id}"))));
Check("concurrent commands retain independent output", concurrentOutputs.All(result =>
    result.Output == $"before:{result.Id}\nafter:{result.Id}"));

var muipSession = new MuipSession { SessionId = "regression-muip", IsAdmin = true, IsAuthorized = true, ExpireTimeStamp = DateTime.Now.AddMinutes(1).ToUnixSec() };
MuipManager.Sessions[muipSession.SessionId] = muipSession;
foreach (var method in new[] { HttpMethod.Get, HttpMethod.Post })
{
    using var response = method == HttpMethod.Get
        ? await client.GetAsync($"/muip/exec_cmd?SessionId={muipSession.SessionId}&Command=%2Fregression-probe%20delayed&TargetUid=0")
        : await client.PostAsJsonAsync("/muip/exec_cmd", new { sessionId = muipSession.SessionId, command = "/regression-probe delayed", targetUid = 0 });
    response.EnsureSuccessStatusCode();
    using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    var output = Encoding.UTF8.GetString(Convert.FromBase64String(json.RootElement.GetProperty("data").GetProperty("message").GetString()!));
    Check($"MUIP {method} awaits output", output.Replace("\r\n", "\n").TrimEnd() == "before\nafter");
}
MuipManager.OnExecuteCommand -= manager.HandleCommandAsync;
Check("missing command handler reports failure", (await MuipManager.ExecuteCommandAsync(muipSession.SessionId, "/help", 0)).Code != 0);
MuipManager.OnExecuteCommand += manager.HandleCommandAsync;
MuipManager.Sessions.TryRemove(muipSession.SessionId, out _);

var playerSession = new MuipSession { SessionId = "regression-player", IsAdmin = false, IsAuthorized = true, ExpireTimeStamp = DateTime.Now.AddMinutes(1).ToUnixSec() };
MuipManager.Sessions[playerSession.SessionId] = playerSession;
var playerResult = MuipManager.ExecuteCommand(playerSession.SessionId, "/help", 0);
Check("non-admin zero UID cannot become console", Encoding.UTF8.GetString(Convert.FromBase64String(playerResult.Data!.Message!)).Contains(I18NManager.Translate("Game.Command.Notice.TargetNotFound", "0")));
MuipManager.Sessions.TryRemove(playerSession.SessionId, out _);

for (var i = 0; i < 3; i++)
{
    var timer = Stopwatch.StartNew();
    using var response = await client.GetAsync("/admin/api/maintenance");
    response.EnsureSuccessStatusCode();
    using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    var data = json.RootElement.GetProperty("data");
    var total = data.GetProperty("machine_total_mb").GetDouble();
    var available = data.GetProperty("machine_available_mb").GetDouble();
    Console.WriteLine($"MEASURE maintenance request {i + 1}: {timer.Elapsed.TotalMilliseconds:F1} ms; memory {available:F0}/{total:F0} MB available");
    Check($"maintenance sample {i + 1} has valid memory", total > 0 && available >= 0 && available <= total);
}
Check("temporary command sessions are removed", MuipManager.Sessions.Count == 0);
await app.StopAsync();
Console.WriteLine($"Regression failures: {failures.Count}");
Environment.ExitCode = failures.Count == 0 ? 0 : 1;

sealed class ProbeCommand : ICommand
{
    public Connection? LastTarget;
    public bool LastExplicit;

    public object? Run(CommandArg arg)
    {
        LastTarget = arg.Target;
        LastExplicit = arg.IsTargetExplicit;
        if (arg.Args.Contains("delayed")) return Delayed(arg);
        if (arg.Args.Contains("throws")) return Throws();
        return arg.SendMsg($"target={arg.Target?.Player?.Uid ?? 0};explicit={arg.IsTargetExplicit}");
    }

    private static async ValueTask Delayed(CommandArg arg)
    {
        var suffix = arg.Args.Count > 1 ? $":{arg.Args[1]}" : "";
        await arg.SendMsg("before" + suffix);
        await Task.Delay(75);
        await arg.SendMsg("after" + suffix);
    }

    private static async Task Throws()
    {
        await Task.Delay(75);
        throw new InvalidOperationException("Intentional regression probe failure");
    }
}

sealed class TestConnection : Connection
{
    public TestConnection(int uid) : base(null!, new IPEndPoint(IPAddress.Loopback, uid))
    {
        Player = new PlayerInstance(new PlayerData { Uid = uid, Name = $"Fixture {uid}" }) { Uid = uid };
        State = SessionStateEnum.ACTIVE;
    }

    public override void Start() { }
}
