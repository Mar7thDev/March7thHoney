using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightGetDataScRsp : BasePacket
{
    public PacketGridFightGetDataScRsp(PlayerInstance player) : base(CmdIds.GridFightGetDataScRsp)
    {
        var proto = new GridFightGetDataScRsp
        {
            RogueGetInfo = player.GridFightManager!.ToProto()
        };

        if (player.GridFightManager!.GridFightInstance != null)
            proto.FightCurrentInfo = player.GridFightManager!.GridFightInstance.ToProto();

        SetData(proto);
    }
}