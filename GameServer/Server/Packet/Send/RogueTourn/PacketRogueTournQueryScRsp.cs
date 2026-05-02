using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueTourn;

public class PacketRogueTournQueryScRsp : BasePacket
{
    public PacketRogueTournQueryScRsp(PlayerInstance player) : base(CmdIds.RogueTournQueryScRsp)
    {
        var proto = new RogueTournQueryScRsp
        {
            RogueGetInfo = player.RogueTournManager!.ToProto()
        };

        SetData(proto);
    }
}