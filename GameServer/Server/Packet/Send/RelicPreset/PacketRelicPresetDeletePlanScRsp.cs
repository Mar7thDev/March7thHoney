using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RelicPreset;

public class PacketRelicPresetDeletePlanScRsp : BasePacket
{
    public PacketRelicPresetDeletePlanScRsp(uint uniqueId, Retcode retcode = Retcode.RetSucc)
        : base(CmdIds.RelicPresetDeletePlanScRsp)
    {
        var proto = new RelicPresetDeletePlanScRsp
        {
            UniqueId = uniqueId,
            Retcode = (uint)retcode
        };

        SetData(proto);
    }
}
