using March7thHoney.Internationalization;

namespace March7thHoney.Command.Command.Cmd;

[CommandInfo("calyx", "Game.Command.Calyx.Desc", "Game.Command.Calyx.Usage")]
public class CommandCalyx : ICommand
{
    [CommandDefault]
    public async ValueTask Default(CommandArg arg)
    {
        var player = arg.Target?.Player;
        if (player == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        var tokens = arg.Raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Calyx.Usage"));
            return;
        }

        var mode = tokens[0].ToLowerInvariant();
        if (mode == "off")
        {
            player.CalyxOverrideManager!.Disable();
            await arg.SendMsg(I18NManager.Translate("Game.Command.Calyx.ToggleOff"));
            return;
        }

        if (mode == "on")
        {
            if (tokens.Length < 2 || !int.TryParse(tokens[1], out var index))
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.Calyx.MissingIndex"));
                return;
            }

            var (ok, error, fileName) = await player.CalyxOverrideManager!.LoadFromIndex(index, arg.SendMsg);
            if (!ok)
                await arg.SendMsg(error ?? I18NManager.Translate("Game.Command.Calyx.LoadFailed"));
            else
                await arg.SendMsg(I18NManager.Translate("Game.Command.Calyx.ToggleOn", fileName ?? ""));
            return;
        }

        await arg.SendMsg(I18NManager.Translate("Game.Command.Calyx.InvalidMode", mode));
    }
}
