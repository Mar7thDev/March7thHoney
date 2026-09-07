using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Tutorial;

public class PacketUnlockTutorialScRsp : BasePacket
{
    public PacketUnlockTutorialScRsp(uint tutorialId, TutorialStatus status = TutorialStatus.TutorialUnlock) : base(
        CmdIds.UnlockTutorialScRsp)
    {
        var proto = new UnlockTutorialScRsp
        {
            Tutorial = new Proto.Tutorial
            {
                Id = tutorialId,
                Status = status
            }
        };
        SetData(proto);
    }
}
