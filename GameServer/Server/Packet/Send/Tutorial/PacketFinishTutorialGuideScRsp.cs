using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Tutorial;

public class PacketFinishTutorialGuideScRsp : BasePacket
{
    public PacketFinishTutorialGuideScRsp(uint groupId, TutorialGuideType type = TutorialGuideType.TutorialGuideDefault)
        : base(CmdIds.FinishTutorialGuideScRsp)
    {
        var rsp = new FinishTutorialGuideScRsp
        {
            TutorialGuide = new TutorialGuide
            {
                Id = groupId,
                Status = TutorialStatus.TutorialFinish,
                Type = type
            },
            Reward = new ItemList()
        };

        rsp.Reward.ItemList_.Add(new Proto.Item
        {
            ItemId = 1,
            Num = 1
        });

        SetData(rsp);
    }
}
