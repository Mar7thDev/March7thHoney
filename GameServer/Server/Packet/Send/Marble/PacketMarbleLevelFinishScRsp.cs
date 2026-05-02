using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Marble;

public class PacketMarbleLevelFinishScRsp : BasePacket
{
    public PacketMarbleLevelFinishScRsp(uint levelId) : base(CmdIds.MarbleLevelFinishScRsp)
    {
        var proto = new MarbleLevelFinishScRsp
        {
            MarbleLevelId = levelId
        };

        SetData(proto);
    }
}