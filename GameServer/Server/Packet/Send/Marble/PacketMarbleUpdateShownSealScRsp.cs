using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Marble;

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