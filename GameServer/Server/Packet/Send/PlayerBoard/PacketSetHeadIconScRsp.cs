using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.PlayerBoard;

public class PacketSetHeadIconScRsp : BasePacket
{
    public PacketSetHeadIconScRsp(PlayerInstance player) : base(CmdIds.SetHeadIconScRsp)
    {
        var proto = new SetHeadIconScRsp
        {
            CurrentHeadIconId = (uint)player.Data.HeadIcon
        };
        SetData(proto);
    }
}