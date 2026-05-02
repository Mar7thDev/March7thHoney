using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Rogue;

public class PacketQuitRogueScRsp : BasePacket
{
    public PacketQuitRogueScRsp(PlayerInstance player) : base(CmdIds.QuitRogueScRsp)
    {
        var proto = new QuitRogueScRsp
        {
            RogueGameInfo = player.RogueManager!.ToProto()
        };

        SetData(proto);
    }
}