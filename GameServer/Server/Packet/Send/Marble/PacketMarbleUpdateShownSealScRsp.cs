using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Marble;

public class PacketMarbleUpdateShownSealScRsp : BasePacket
{
    public PacketMarbleUpdateShownSealScRsp(ICollection<uint> sealList) : base(CmdIds.MarbleUpdateShownSealScRsp)
    {
        var proto = new MarbleUpdateShownSealScRsp
        {
            UpdateSealList = { sealList }
        };

        SetData(proto);
    }
}