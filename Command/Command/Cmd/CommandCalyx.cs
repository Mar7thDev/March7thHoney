using March7thHoney.Internationalization;

namespace March7thHoney.Command.Command.Cmd;

[CommandInfo("calyx", "Game.Command.Calyx.Desc", "Game.Command.Calyx.Usage", permission: CommandPermissions.Calyx)]
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
            var manager = player.CalyxOverrideManager!;
            if (manager.Data.CachedJson?.BattleConfig == null)
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.Calyx.NoCachedData"));
                return;
            }

            manager.Enable();
            await arg.SendMsg(I18NManager.Translate("Game.Command.Calyx.ToggleOn"));
            return;
        }

        if (mode == "modify")
        {
            var manager = player.CalyxOverrideManager!;
            if (!manager.IsActive)
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.Calyx.ModifyRequiresActive"));
                return;
            }

            if (manager.IsCurrentBattleChallengePeak)
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.Calyx.ModifyChallengePeakUnsupported"));
                return;
            }

            if (tokens.Length != 4 || !int.TryParse(tokens[1], out var waveIndex) || waveIndex <= 0 ||
                !int.TryParse(tokens[2], out var monsterIndex) || monsterIndex <= 0)
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.Calyx.ModifyUsage"));
                return;
            }

            if (!uint.TryParse(tokens[3], out var hp) || hp == 0)
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.Calyx.ModifyInvalidHp"));
                return;
            }

            if (!manager.TrySetMonsterHp(waveIndex, monsterIndex, hp, out var monsterId))
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.Calyx.ModifyInvalidPosition",
                    waveIndex.ToString(), monsterIndex.ToString()));
                return;
            }

            await arg.SendMsg(I18NManager.Translate("Game.Command.Calyx.ModifySuccess",
                waveIndex.ToString(), monsterIndex.ToString(), monsterId.ToString(), hp.ToString()));
            return;
        }

        await arg.SendMsg(I18NManager.Translate("Game.Command.Calyx.InvalidMode", mode));
    }
}
