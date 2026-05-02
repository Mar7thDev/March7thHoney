using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Scene;

public class PacketActivateFarmElementScRsp : BasePacket
{
    public PacketActivateFarmElementScRsp(uint entityId, PlayerInstance player) : base(CmdIds.ActivateFarmElementScRsp)
    {
        var proto = new ActiveFarmElementScRsp
        {
            EntityId = entityId,
            WorldLevel = (uint)player.Data.WorldLevel
        };

        SetData(proto);
    }
}

