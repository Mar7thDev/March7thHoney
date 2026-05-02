using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueMagic;

public class PacketRogueMagicQueryScRsp : BasePacket
{
    public PacketRogueMagicQueryScRsp(PlayerInstance player) : base(CmdIds.RogueMagicQueryScRsp)
    {
        var proto = new RogueMagicQueryScRsp
        {
            RogueGetInfo = player.RogueMagicManager!.ToGetInfo()
        };

        SetData(proto);
    }
}