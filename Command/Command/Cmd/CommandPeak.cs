using March7thHoney.Data;
using March7thHoney.GameServer.Server.Packet.Send.ChallengePeak;
using March7thHoney.Internationalization;

namespace March7thHoney.Command.Command.Cmd;

[CommandInfo("peak", "Game.Command.Peak.Desc", "Game.Command.Peak.Usage", permission: CommandPermissions.Peak)]
public class CommandPeak : ICommand
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

        if (arg.BasicArgs.Count < 1 || !int.TryParse(arg.BasicArgs[0], out var groupId))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Peak.Usage"));
            return;
        }

        if (!player.ChallengePeakManager!.TrySetCurrentGroupId(groupId))
        {
            var available = string.Join(", ", GameData.GetAvailableChallengePeakGroupIds());
            await arg.SendMsg(I18NManager.Translate("Game.Command.Peak.InvalidGroup",
                groupId.ToString(),
                string.IsNullOrEmpty(available) ? "-" : available));
            return;
        }

        await arg.Target!.SendPacket(new PacketGetChallengePeakDataScRsp(player));
        await arg.Target.SendPacket(new PacketChallengePeakGroupDataUpdateScNotify(
            player.ChallengePeakManager.GetChallengePeakInfo(groupId)));

        await arg.SendMsg(I18NManager.Translate("Game.Command.Peak.Switched", groupId.ToString()));
    }
}
