using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Activity;

public class PacketGetMaterialSubmitActivityDataScRsp : BasePacket
{
    public PacketGetMaterialSubmitActivityDataScRsp(IEnumerable<KPMAKCHCDMH> entries)
        : base(CmdIds.GetMaterialSubmitActivityDataScRsp)
    {
        var proto = new GetMaterialSubmitActivityDataScRsp();
        proto.JJNFLKKAOFI.AddRange(entries);
        SetData(proto);
    }
}
