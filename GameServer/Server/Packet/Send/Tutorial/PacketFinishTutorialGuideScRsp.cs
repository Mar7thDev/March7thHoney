using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Tutorial;

public class PacketFinishTutorialGuideScRsp : BasePacket
{
    public PacketFinishTutorialGuideScRsp(uint tutorialId, TutorialGuideType type) : base(CmdIds.FinishTutorialGuideScRsp)
    {
        var rsp = new FinishTutorialGuideScRsp
        {
            TutorialGuide = new TutorialGuide
            {
                Id = tutorialId,
                Status = TutorialStatus.TutorialFinish,
                Type = type
            },
            Reward = new ItemList()
        };

        SetData(rsp);
    }
}
