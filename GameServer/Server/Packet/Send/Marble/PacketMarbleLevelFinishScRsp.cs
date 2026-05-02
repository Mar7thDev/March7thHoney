using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Marble;

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