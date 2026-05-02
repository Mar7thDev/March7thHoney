using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Message;

public class PacketFinishPerformSectionIdScRsp : BasePacket
{
    public PacketFinishPerformSectionIdScRsp(uint sectionId) : base(CmdIds.FinishPerformSectionIdScRsp)
    {
        var proto = new FinishPerformSectionIdScRsp
        {
            SectionId = sectionId
        };

        SetData(proto);
    }
}