using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Activity;

public class PacketSubmitMaterialSubmitActivityMaterialScRsp : BasePacket
{
    public PacketSubmitMaterialSubmitActivityMaterialScRsp(Retcode retcode, KPMAKCHCDMH? entry)
        : base(CmdIds.SubmitMaterialSubmitActivityMaterialScRsp)
    {
        var proto = new SubmitMaterialSubmitActivityMaterialScRsp { Retcode = (uint)retcode };
        if (entry != null) proto.MEAEIKFLBPJ = entry;
        SetData(proto);
    }
}
