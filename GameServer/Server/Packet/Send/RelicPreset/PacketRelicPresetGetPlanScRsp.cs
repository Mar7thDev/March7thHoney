using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RelicPreset;

public class PacketRelicPresetGetPlanScRsp : BasePacket
{
    public PacketRelicPresetGetPlanScRsp(uint avatarId, List<PFCKBFLDHNI> planList)
        : base(CmdIds.RelicPresetGetPlanScRsp)
    {
        var proto = new RelicPresetGetPlanScRsp
        {
            AvatarId = avatarId,
            RelicPlanList = { planList }
        };

        SetData(proto);
    }
}
