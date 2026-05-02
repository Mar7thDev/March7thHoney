using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.MapRotation;

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