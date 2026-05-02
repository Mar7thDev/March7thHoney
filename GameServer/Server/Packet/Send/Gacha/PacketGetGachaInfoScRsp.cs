using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Gacha;

public class PacketGetGachaInfoScRsp : BasePacket
{
    public PacketGetGachaInfoScRsp(PlayerInstance player) : base(CmdIds.GetGachaInfoScRsp)
    {
        SetData(player.GachaManager!.ToProto());
    }
}