using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Rogue;

public class PacketGetRogueInfoScRsp : BasePacket
{
    public PacketGetRogueInfoScRsp(PlayerInstance player) : base(CmdIds.GetRogueInfoScRsp)
    {
        var proto = new GetRogueInfoScRsp
        {
            RogueGameInfo = player.RogueManager!.ToProto()
        };

        SetData(proto);
    }
}