using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.MapRotation;

public class PacketRemoveRotaterScRsp : BasePacket
{
    public PacketRemoveRotaterScRsp(PlayerInstance player, RemoveRotaterCsReq req) : base(CmdIds.RemoveRotaterScRsp)
    {
        var proto = new RemoveRotaterScRsp
        {
            EnergyInfo = new RotaterEnergyInfo
            {
                CurNum = (uint)player.ChargerNum,
                MaxNum = 5
            },
            RotaterData = req.RotaterData
        };

        SetData(proto);
    }
}