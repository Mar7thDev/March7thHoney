using March7thHoney.GameServer.Server;
using March7thHoney.GameServer.Server.Packet.Send.Player;
using March7thHoney.Internationalization;
using March7thHoney.Kcp;
using March7thHoney.Util;

namespace March7thHoney.Command.Command.Cmd;

[CommandInfo("anno", "Game.Command.Anno.Desc", "Game.Command.Anno.Usage", permission: CommandPermissions.Anno)]
public class CommandAnno : ICommand
{
    [CommandDefault]
    public async ValueTask Default(CommandArg arg)
    {
        var cfg = ConfigManager.Config.ServerOption.ServerAnnounce;
        var content = string.IsNullOrWhiteSpace(arg.Raw) ? cfg.AnnounceContent : arg.Raw.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        var sent = 0;
        foreach (var connection in March7thHoneyListener.Connections.Values.OfType<Connection>())
        {
            if (!connection.IsOnline || connection.Player == null) continue;
            await connection.SendPacket(new PacketServerAnnounceNotify(content));
            connection.Player.LoginAnnounceSent = true;
            sent++;
        }

        await arg.SendMsg(I18NManager.Translate("Game.Command.Anno.Sent", sent.ToString()));
    }
}
