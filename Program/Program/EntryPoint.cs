using March7thHoney.Command;
using March7thHoney.Command.Command;
using March7thHoney.Configuration;
using March7thHoney.Data;
using March7thHoney.Database;
using March7thHoney.Database.Migrations;
using March7thHoney.Enums;
using March7thHoney.GameServer.Command;
using March7thHoney.GameServer.Server;
using March7thHoney.GameServer.Server.Packet;
using March7thHoney.Internationalization;
using March7thHoney.Kcp;
using March7thHoney.Program.Generator;
using March7thHoney.Program.Handbook;
using March7thHoney.Util;
using March7thHoney.WebServer;
using March7thHoney.WebServer.Handler;
using March7thHoney.WebServer.Server;
using System.Globalization;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace March7thHoney.Program.Program;

public class EntryPoint
{
    private static readonly Logger Logger = new("Program");
    public static readonly DatabaseHelper DatabaseHelper = new();
    public static readonly CommandManager CommandManager = new();
    private static PosixSignalRegistration? _sigtermRegistration;

    public static async Task Main(string[] args)
    {
        IConsole.InitConsole();
        IConsole.RedrawInput(IConsole.Input);
        // Capture log output from the very first line so the admin console's log viewer has the boot history.
        AdminLogBuffer.Attach();
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            Logger.Info(I18NManager.Translate("Server.ServerInfo.Shutdown"));
            PerformCleanup();
        };
        AppDomain.CurrentDomain.UnhandledException += (obj, arg) =>
        {
            Logger.Error(I18NManager.Translate("Server.ServerInfo.UnhandledException", obj.GetType().Name),
                (Exception)arg.ExceptionObject);
            Logger.Info(I18NManager.Translate("Server.ServerInfo.Shutdown"));
            PerformCleanup();
            Environment.Exit(1);
        };

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            Logger.Info(I18NManager.Translate("Server.ServerInfo.CancelKeyPressed"));
            eventArgs.Cancel = true;
            Environment.Exit(0);
        };
        // .NET 10 dropped ProcessExit on window-close/logoff/shutdown; hook the Win32 console control events so the DB save runs (Windows-only kernel32 P/Invoke).
        if (OperatingSystem.IsWindows())
            SetConsoleCtrlHandler(ConsoleCtrlHandlerKeepAlive, true);
        else if (OperatingSystem.IsLinux())
            _sigtermRegistration = PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnSigTerm);
        var time = DateTime.Now;

        // pack the old log
        var logDirectory = new DirectoryInfo(GetConfig().Path.LogPath);
        if (logDirectory.Exists)
        {
            List<string> packed = [];
            foreach (var oldFile in logDirectory.GetFiles().ToArray())
            {
                if (!oldFile.Name.EndsWith(".log")) continue;
                if (oldFile.Name.EndsWith("-debug.log")) continue;
                if (packed.Contains(oldFile.Name)) continue;

                var fileName = oldFile.Name.Replace(".log", "");
                var debugFileName = fileName + "-debug";
                var oldDebugFile = logDirectory.GetFiles(debugFileName + ".log").FirstOrDefault();

                if (oldFile.Exists)
                {
                    var zipFileName = fileName + ".zip";
                    var zipFile = new FileInfo(Path.Combine(GetConfig().Path.LogPath, zipFileName));
                    if (zipFile.Exists) zipFile.Delete();
                    using (var zip = ZipFile.Open(zipFile.FullName, ZipArchiveMode.Create))
                    {
                        zip.CreateEntryFromFile(oldFile.FullName, oldFile.Name);
                        if (oldDebugFile is { Exists: true })
                            zip.CreateEntryFromFile(oldDebugFile.FullName, oldDebugFile.Name);
                    }

                    oldFile.Delete();
                    oldDebugFile?.Delete();
                    packed.Add(oldFile.Name);
                    packed.Add(oldDebugFile?.Name ?? "");
                }
            }
        }

        // Initialize the logfile
        var counter = 0;
        FileInfo file;
        FileInfo zi;
        while (true)
        {
            file = new FileInfo(Path.Combine(GetConfig().Path.LogPath,
                $"{DateTime.Now:yyyy-MM-dd}-{++counter}.log"));
            zi = new FileInfo(Path.Combine(GetConfig().Path.LogPath,
                $"{DateTime.Now:yyyy-MM-dd}-{counter}.zip"));
            if (file is not { Exists: false, Directory: not null }) continue;
            if (zi is not { Exists: false, Directory: not null }) continue;
            file.Directory.Create();
            break;
        }

        var debugFile = new FileInfo(Path.Combine(GetConfig().Path.LogPath,
            $"{DateTime.Now:yyyy-MM-dd}-{counter}-debug.log"));

        Logger.SetLogFile(file);
        Logger.SetDebugLogFile(debugFile);
        // Load the config
        try
        {
            ConfigManager.LoadConfig();
        }
        catch (Exception e)
        {
            Logger.Error(
                I18NManager.Translate("Server.ServerInfo.FailedToLoadItem", I18NManager.Translate("Word.Config")), e);
            Console.ReadLine();
            return;
        }

        // Load the language
        try
        {
            I18NManager.LoadLanguage();
        }
        catch (Exception e)
        {
            Logger.Error(
                I18NManager.Translate("Server.ServerInfo.FailedToLoadItem", I18NManager.Translate("Word.Language")), e);
            Console.ReadLine();
            return;
        }

        // Starting logs after i18n is ready
        Logger.Info(I18NManager.Translate("Server.ServerInfo.StartingServer"));
        Logger.Info(I18NManager.Translate("Server.ServerInfo.LoadingItem", I18NManager.Translate("Word.Config")));
        Logger.Info(I18NManager.Translate("Server.ServerInfo.LoadingItem", I18NManager.Translate("Word.Language")));
        ConfigManager.Logger.Info(
            I18NManager.Translate("Server.ServerInfo.CurrentVersion", GameConstants.GAME_VERSION));
        // Initialize the database
        Task databaseInitTask;
        try
        {
            databaseInitTask = Task.Run(DatabaseHelper.Initialize);

            while (!DatabaseHelper.LoadAccount)
            {
                if (databaseInitTask.IsFaulted)
                    databaseInitTask.GetAwaiter().GetResult();

                await Task.Delay(100);
            }

            Logger.Info(I18NManager.Translate("Server.ServerInfo.LoadedItem",
                I18NManager.Translate("Word.DatabaseAccount")));
        }
        catch (Exception e)
        {
            Logger.Error(
                I18NManager.Translate("Server.ServerInfo.FailedToLoadItem", I18NManager.Translate("Word.Database")), e);
            Console.ReadLine();
            return;
        }

        // Warm protobuf descriptors at startup so the first login doesn't pay the initialization cost.
        var protoDescriptorWarmUp = Task.Run(() => _ = March7thHoney.Proto.StarRailReflection.Descriptor);

        HandlerManager.Init();
        if (ConfigManager.Config.GameServer.UsePacketEncryption)
        {
            Crypto.ClientSecretKey = Crypto.InitEc2b();
            if (Crypto.ClientSecretKey == null) ConfigManager.Config.GameServer.UsePacketEncryption = false;
        }

        Logger.Warn(I18NManager.Translate("Server.ServerInfo.WaitForAllDone"));

        var loadedFromResources = false;
        if (ConfigManager.Config.ServerOption.ServerConfig.RunGateway)
        {
            try
            {
                loadedFromResources = await GameDataStartup.LoadAsync();
            }
            catch (Exception e)
            {
                Logger.Error(
                    I18NManager.Translate("Server.ServerInfo.FailedToLoadItem", I18NManager.Translate("Word.GameData")),
                    e);
                Console.ReadLine();
                return;
            }

            // check option
            if (args.Contains("-generate-tourn"))
            {
                TournRoomGenerator.GenerateFile("RogueTournRoom.json");
                return;
            }
        }

        // Register the command handlers
        try
        {
            CommandManager.RegisterCommands();
        }
        catch (Exception e)
        {
            Logger.Error(
                I18NManager.Translate("Server.ServerInfo.FailedToInitializeItem",
                    I18NManager.Translate("Word.Command")), e);
            Console.ReadLine();
            return;
        }

        CommandExecutor.OnRunCommand += (sender, e) => { CommandManager.HandleCommand(e, sender); };

        MuipManager.CommandHandler = CommandManager.HandleCommandAsync;
        MuipManager.CommandCatalogProvider = CommandManager.BuildCatalog;

        // Generate handbooks only when this startup reads raw resources.
        if (loadedFromResources)
        {
            var handbookThread = new Thread(HandbookGenerator.GenerateAll)
            {
                IsBackground = true,
                Name = "HandbookGenerator",
                Priority = ThreadPriority.Lowest
            };
            handbookThread.Start();
        }

        if (!DatabaseHelper.LoadAllData)
        {
            Logger.Warn(I18NManager.Translate("Server.ServerInfo.WaitForAllDone"));
            try
            {
                while (!DatabaseHelper.LoadAllData) // wait for all data to be loaded
                {
                    if (databaseInitTask.IsFaulted)
                        databaseInitTask.GetAwaiter().GetResult();

                    await Task.Delay(100);
                }
            }
            catch (Exception e)
            {
                Logger.Error(
                    I18NManager.Translate("Server.ServerInfo.FailedToLoadItem",
                        I18NManager.Translate("Word.Database")), e);
                Console.ReadLine();
                return;
            }

            Logger.Info(I18NManager.Translate("Server.ServerInfo.LoadedItem", I18NManager.Translate("Word.Database")));
        }

        AuthRateLimiter.Initialize();

        ServerUtils.InitializeHandlers();

        FormalAvatarMigration.Run(DatabaseHelper);

        if (args.Contains("--upgrade-database")) DatabaseHelper.UpgradeDatabase();

        await protoDescriptorWarmUp;
        // The dispatch and game ports open only now, so no client reaches a server whose game data, commands, mission
        // handlers or save migrations are still loading.
        WebProgram.Main([], GetConfig().HttpServer.Port, GetConfig().HttpServer.GetBindDisplayAddress());
        Logger.Info(I18NManager.Translate("Server.ServerInfo.ServerRunning", I18NManager.Translate("Word.Dispatch"),
            GetConfig().HttpServer.GetDisplayAddress()));

        // The packet name maps back logging and the fallback replies, so they exist before any connection.
        March7thHoneyConnection.InitPacketNameMap();

        if (ConfigManager.Config.ServerOption.ServerConfig.RunGateway)
        {
            March7thHoneyListener.CreateConnection = (conversation, remote) => new Connection(conversation, remote);
            March7thHoneyListener.StartListener();
        }

        var elapsed = DateTime.Now - time;
        Logger.Info(I18NManager.Translate("Server.ServerInfo.ServerStarted",
            Math.Round(elapsed.TotalSeconds, 2).ToString(CultureInfo.InvariantCulture)));

        if (GetConfig().ServerOption.EnableMission)
            Logger.Warn(I18NManager.Translate("Server.ServerInfo.MissionEnabled"));

        await LifecycleNotifier.NotifyStartedAsync(GameConstants.GAME_VERSION);

        IConsole.OnConsoleExcuteCommand += command =>
        {
            CommandManager.HandleCommand(command, new ConsoleCommandSender(Logger));
            IConsole.RedrawInput(IConsole.Input);
        };

        IConsole.ListenConsole();
        GC.KeepAlive(_sigtermRegistration);
    }

    public static ConfigContainer GetConfig()
    {
        return ConfigManager.Config;
    }

    private delegate bool ConsoleCtrlHandlerRoutine(uint ctrlType);

    private static readonly ConsoleCtrlHandlerRoutine ConsoleCtrlHandlerKeepAlive = OnConsoleCtrl;

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandlerRoutine handler, bool add);

    // CTRL_CLOSE(2)/LOGOFF(5)/SHUTDOWN(6): exit cleanly so PerformCleanup saves; Ctrl+C/Break stay with CancelKeyPress.
    private static bool OnConsoleCtrl(uint ctrlType)
    {
        if (ctrlType is 2 or 5 or 6) Environment.Exit(0);
        return false;
    }

    private static void OnSigTerm(PosixSignalContext context)
    {
        context.Cancel = true;
        Environment.Exit(0);
    }

    private static void PerformCleanup()
    {
        var notificationTask = LifecycleNotifier.NotifyMaintenanceAsync();
        // Stopping a live connection on shutdown can throw (disposed CancellationTokenSource race); never let it skip the save.
        try { March7thHoneyListener.GetSnapshot().ForEach(x => x.Stop()); }
        catch (Exception e) { Logger.Error("Cleanup: failed to stop connections", e); }
        DatabaseHelper.SaveDatabase();
        notificationTask.GetAwaiter().GetResult();
    }
}
