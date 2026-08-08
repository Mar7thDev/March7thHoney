using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Player;

public class PacketCancelAutoConversionCacheScRsp : BasePacket
{
    public PacketCancelAutoConversionCacheScRsp() : base(CmdIds.INKEEEKKNKF)
    {
        SetData(new INKEEEKKNKF());
    }
}
