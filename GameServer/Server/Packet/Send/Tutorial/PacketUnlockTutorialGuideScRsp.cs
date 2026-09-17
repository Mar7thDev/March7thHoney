using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Tutorial;

public class PacketUnlockTutorialGuideScRsp : BasePacket
{
    public PacketUnlockTutorialGuideScRsp(uint groupId, TutorialStatus status = TutorialStatus.TutorialUnlock,
        TutorialGuideType type = TutorialGuideType.TutorialGuideDefault) : base(CmdIds.UnlockTutorialGuideScRsp)
    {
        var proto = new UnlockTutorialGuideScRsp
        {
            TutorialGuide = new TutorialGuide
            {
                Id = groupId,
                Status = status,
                Type = type
            }
        };
        SetData(proto);
    }
}
