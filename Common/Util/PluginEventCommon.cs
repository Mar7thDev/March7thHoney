namespace March7thHoney.Util;

public static class PluginEventCommon
{
    #region Common

    public delegate void OnConsoleLogHandler(string message);

    public delegate void OnLogEntryHandler(string module, LoggerLevel level, string message);

    #endregion

    #region Event

    public static event OnConsoleLogHandler? OnConsoleLog;

    /// <summary>Structured form of <see cref="OnConsoleLog" /> (module, level and message kept separate).</summary>
    public static event OnLogEntryHandler? OnLogEntry;

    #endregion

    public static void InvokeOnConsoleLog(string message)
    {
        OnConsoleLog?.Invoke(message);
    }

    public static void InvokeOnLogEntry(string module, LoggerLevel level, string message)
    {
        OnLogEntry?.Invoke(module, level, message);
    }
}