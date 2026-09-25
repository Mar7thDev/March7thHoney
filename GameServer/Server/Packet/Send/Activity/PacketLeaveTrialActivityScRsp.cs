using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Activity;

public class PacketLeaveTrialActivityScRsp : BasePacket
{
    public PacketLeaveTrialActivityScRsp(uint stageId) : base(CmdIds.LeaveTrialActivityScRsp)
    {
        SetData(new LeaveTrialActivityScRsp
        {
            StageId = stageId
        });
    }
}
