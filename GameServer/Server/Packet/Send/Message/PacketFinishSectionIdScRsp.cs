using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Message;

public class PacketFinishSectionIdScRsp : BasePacket
{
    public PacketFinishSectionIdScRsp(uint sectionId) : base(CmdIds.FinishSectionIdScRsp)
    {
        var proto = new FinishSectionIdScRsp
        {
            SectionId = sectionId
        };

        SetData(proto);
    }
}