using March7thHoney.Util;

namespace March7thHoney.Command;

public interface ICommandSender
{
    public ValueTask SendMsg(string msg);

    public bool HasPermission(string permission);

    public int GetSender();

    /// <summary>
    ///     Language-file code (CHS/CHT/EN/…) this sender's replies should be localized to, or null for the server
    ///     default. Drives the per-command language override so the friend-list bot answers each player in the
    ///     language their client reported at login. Console/admin senders keep the server default.
    /// </summary>
    public string? Language => null;

    /// <summary>
    ///     True for senders that act like the server console: no player of their own, commands run without an
    ///     implicit player and "@uid" picks a target. Admin requests may also supply a target via GetSender().
    /// </summary>
    public bool IsConsole => false;
}

public class ConsoleCommandSender(Logger logger) : ICommandSender
{
    public bool IsConsole => true;

    public async ValueTask SendMsg(string msg)
    {
        logger.Info(msg);
        await Task.CompletedTask;
    }

    public bool HasPermission(string permission)
    {
        return true;
    }

    public int GetSender()
    {
        return 0;
    }
}
