using System.Diagnostics;
using System.Threading.Channels;
using March7thHoney.Internationalization;
using Spectre.Console;

namespace March7thHoney.Util;

public class Logger(string moduleName)
{
    private static FileInfo? _logFile;
    private static FileInfo? _debugLogFile;
    private static readonly object Lock = new();
    private static readonly Channel<ConsoleLogEntry> ConsoleLog = CreateConsoleLog();
    private static int _consolePending; // queued or being written
    private static volatile bool _consoleDirect;

    private readonly record struct ConsoleLogEntry(string Time, string Module, LoggerLevel Level, string Message);

    public void Log(string message, LoggerLevel level)
    {
        lock (Lock)
        {
            var time = DateTime.Now.ToString("HH:mm:ss");
            // Resource loader warnings stay out of the console and the main log; they are kept in the debug log only.
            if (level == LoggerLevel.WARN && moduleName is "ResMgr" or "ResCache")
            {
                WriteToDebugFile($"[{time}][{moduleName}][{level}] {message}");
                return;
            }

            var entry = new ConsoleLogEntry(time, moduleName, level, message);
            if (_consoleDirect)
            {
                WriteConsole(entry);
            }
            else
            {
                Interlocked.Increment(ref _consolePending);
                if (!ConsoleLog.Writer.TryWrite(entry)) Interlocked.Decrement(ref _consolePending);
            }

            var logMessage = $"[{time}][{moduleName}][{level}] {message}";
            PluginEventCommon.InvokeOnConsoleLog(logMessage);
            try
            {
                PluginEventCommon.InvokeOnLogEntry(moduleName, level, message);
            }
            catch
            {
                // A misbehaving subscriber must never break logging.
            }

            if (level == LoggerLevel.DEBUG)
            {
                WriteToDebugFile(logMessage);
                return;
            }

            WriteToFile(logMessage);
        }
    }

    private static Channel<ConsoleLogEntry> CreateConsoleLog()
    {
        // A paused Windows console or a full stdout pipe must not block request handlers: lines are queued and
        // written by one background reader. The file logs and the log subscribers get every entry even when the
        // display falls behind and the oldest queued lines are dropped.
        var queue = Channel.CreateBounded<ConsoleLogEntry>(new BoundedChannelOptions(512)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.DropOldest
        }, _ => Interlocked.Decrement(ref _consolePending));
        _ = Task.Run(async () =>
        {
            await foreach (var entry in queue.Reader.ReadAllAsync())
            {
                WriteConsole(entry);
                Interlocked.Decrement(ref _consolePending);
            }
        });
        // The reader ends with the process. At exit, give the queued lines (the details of an unhandled exception
        // are the usual case) a moment to appear, then write directly so later shutdown messages are not lost.
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            var waited = Stopwatch.StartNew();
            while (Volatile.Read(ref _consolePending) > 0 && waited.ElapsedMilliseconds < 2000) Thread.Sleep(10);
            _consoleDirect = true;
        };
        return queue;
    }

    private static void WriteConsole(ConsoleLogEntry entry)
    {
        var redirected = $"[{entry.Time}][{entry.Module}] {entry.Message}";
        try
        {
            if (Console.IsOutputRedirected)
            {
                Console.WriteLine(redirected);
                return;
            }

            var levelColor = entry.Level switch
            {
                LoggerLevel.INFO => "#7AA8DD",
                LoggerLevel.WARN => "#F6C177",
                LoggerLevel.ERROR => "#EB6F92",
                LoggerLevel.FATAL => "#C73E1D",
                LoggerLevel.DEBUG => "#9CCFD8",
                _ => "#9992C7"
            };
            var colored = $"[grey70][[{entry.Time}]][/] [#60BAEE][[{Markup.Escape(entry.Module)}]][/] " +
                          $"[{levelColor}]{entry.Level}[/] {Markup.Escape(entry.Message)}";
            var plain = $"[{entry.Time}] [{entry.Module}] {entry.Level} {entry.Message}";
            IConsole.WriteExternalLine(() => AnsiConsole.MarkupLine(colored), plain);
        }
        catch
        {
            try
            {
                Console.WriteLine(redirected);
            }
            catch
            {
                // A detached or closed terminal must not stop the reader; the file log already has the entry.
            }
        }
    }

    public void Info(string message, Exception? e = null)
    {
        Log(message, LoggerLevel.INFO);
        if (e != null)
        {
            Log(e.Message, LoggerLevel.INFO);
            Log(e.StackTrace ?? string.Empty, LoggerLevel.INFO);
        }
    }

    public void InfoI18n(string key, params string[] args)
    {
        Info(I18NManager.Translate(key, args));
    }

    public void Warn(string message, Exception? e = null)
    {
        Log(message, LoggerLevel.WARN);
        if (e != null)
        {
            Log(e.Message, LoggerLevel.WARN);
            Log(e.StackTrace ?? string.Empty, LoggerLevel.WARN);
        }
    }

    public void WarnI18n(string key, params string[] args)
    {
        Warn(I18NManager.Translate(key, args));
    }

    public void Error(string message, Exception? e = null)
    {
        Log(message, LoggerLevel.ERROR);
        if (e != null)
        {
            Log(e.Message, LoggerLevel.ERROR);
            Log(e.StackTrace ?? string.Empty, LoggerLevel.ERROR);
        }
    }

    public void ErrorI18n(string key, params string[] args)
    {
        Error(I18NManager.Translate(key, args));
    }

    public void Fatal(string message, Exception? e = null)
    {
        Log(message, LoggerLevel.FATAL);
        if (e != null)
        {
            Log(e.Message, LoggerLevel.FATAL);
            Log(e.StackTrace ?? string.Empty, LoggerLevel.FATAL);
        }
    }

    public void Debug(string message, Exception? e = null)
    {
        Log(message, LoggerLevel.DEBUG);
        if (e != null)
        {
            Log(e.Message, LoggerLevel.DEBUG);
            Log(e.StackTrace ?? string.Empty, LoggerLevel.DEBUG);
        }
    }

    public static void SetLogFile(FileInfo file) => _logFile = file;
    public static void SetDebugLogFile(FileInfo file) => _debugLogFile = file;
    public static FileInfo? CurrentLogFile => _logFile;
    public static FileInfo? CurrentDebugLogFile => _debugLogFile;

    public static void WriteToFile(string message)
    {
        try
        {
            if (_logFile == null) throw new Exception("LogFile is not set");
            _logFile.Directory?.Create();
            using var sw = _logFile.AppendText();
            sw.WriteLine(message);
        }
        catch
        {
        }
    }

    public static void WriteToDebugFile(string message)
    {
        try
        {
            if (_debugLogFile == null) throw new Exception("DebugLogFile is not set");
            _debugLogFile.Directory?.Create();
            using var sw = _debugLogFile.AppendText();
            sw.WriteLine(message);
        }
        catch
        {
        }
    }

    public static Logger GetByClassName()
    {
        return new Logger(new StackTrace().GetFrame(1)?.GetMethod()?.ReflectedType?.Name ?? string.Empty);
    }
}

public enum LoggerLevel
{
    INFO,
    WARN,
    ERROR,
    FATAL,
    DEBUG
}

