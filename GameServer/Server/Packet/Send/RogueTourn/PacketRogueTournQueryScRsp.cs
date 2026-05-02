using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueTourn;

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