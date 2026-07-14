using March7thHoney.Data;
using March7thHoney.GameServer.Server;
using March7thHoney.GameServer.Server.Packet.Send.ChallengePeak;
using March7thHoney.Internationalization;
using March7thHoney.Kcp;

namespace March7thHoney.Command.Command.Cmd;

[CommandInfo("peak", "Game.Command.Peak.Desc", "Game.Command.Peak.Usage", permission: CommandPermissions.Peak)]
public class CommandPeak : ICommand
{
    [CommandDefault]
    public async ValueTask Default(CommandArg arg)
    {
        if (arg.BasicArgs.Count < 1 || !int.TryParse(arg.BasicArgs[0], out var groupId))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Peak.Usage"));
            return;
        }

        if (!GameData.TrySetCurrentChallengePeakGroupId(groupId))
        {
            var available = string.Join(", ", GameData.GetAvailableChallengePeakGroupIds());
            await arg.SendMsg(I18NManager.Translate("Game.Command.Peak.InvalidGroup",
                groupId.ToString(),
                string.IsNullOrEmpty(available) ? "-" : available));
            return;
        }

        var refreshed = 0;
        foreach (var connection in March7thHoneyListener.GetSnapshot().OfType<Connection>())
        {
            if (!connection.IsOnline || connection.Player?.ChallengePeakManager == null) continue;
            await connection.SendPacket(new PacketGetChallengePeakDataScRsp(connection.Player));
            await connection.SendPacket(new PacketChallengePeakGroupDataUpdateScNotify(
                connection.Player.ChallengePeakManager.GetChallengePeakInfo(groupId)));
            refreshed++;
        }

        await arg.SendMsg(I18NManager.Translate("Game.Command.Peak.Switched",
            groupId.ToString(), refreshed.ToString()));
    }
}
