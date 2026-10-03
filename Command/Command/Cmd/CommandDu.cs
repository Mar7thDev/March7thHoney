using March7thHoney.Internationalization;

namespace March7thHoney.Command.Command.Cmd;

/// <summary>
///     Divergent Universe (RogueTourn) run maintenance. reset is the in-game escape hatch for a run the client
///     cannot sync (it blocks the Divergent Universe entry screen): only the live run is dropped, account progress
///     (exp, claimed rewards, archives, talents) stays. blessall grants the live run every blessing its generation
///     pools, through the same bookkeeping and notifies a real blessing pick uses.
/// </summary>
[CommandInfo("du", "Game.Command.Du.Desc", "Game.Command.Du.Usage", ["divergentuniverse", "tourn"],
    permission: CommandPermissions.Du)]
public class CommandDu : ICommand
{
    [CommandMethod("0 reset")]
    public async ValueTask Reset(CommandArg arg)
    {
        var manager = arg.Target?.Player?.RogueTournManager;
        if (manager == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        var hadRun = await manager.ResetRun();
        await arg.SendMsg(I18NManager.Translate(hadRun ? "Game.Command.Du.Reset" : "Game.Command.Du.NoRun"));
    }

    [CommandMethod("0 blessall")]
    public async ValueTask BlessAll(CommandArg arg)
    {
        var manager = arg.Target?.Player?.RogueTournManager;
        if (manager == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        if (manager.RunInstance == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Du.NoLiveRun"));
            return;
        }

        var granted = await manager.GrantAllBlessings();
        await arg.SendMsg(granted > 0
            ? I18NManager.Translate("Game.Command.Du.BlessAll", granted.ToString())
            : I18NManager.Translate("Game.Command.Du.BlessNone"));
    }
}
