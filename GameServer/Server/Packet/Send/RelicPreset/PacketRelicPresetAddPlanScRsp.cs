using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RelicPreset;

public class PacketRelicPresetAddPlanScRsp : BasePacket
{
    public PacketRelicPresetAddPlanScRsp(PFCKBFLDHNI plan) : base(CmdIds.RelicPresetAddPlanScRsp)
    {
        var proto = new RelicPresetAddPlanScRsp
        {
            RelicPlan = plan
        };

        SetData(proto);
    }

    public PacketRelicPresetAddPlanScRsp(Retcode retcode) : base(CmdIds.RelicPresetAddPlanScRsp)
    {
        SetData(new RelicPresetAddPlanScRsp
        {
            Retcode = (uint)retcode
        });
    }
}
