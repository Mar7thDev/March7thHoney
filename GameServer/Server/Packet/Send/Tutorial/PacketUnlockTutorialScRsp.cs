using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Tutorial;

public class PacketUnlockTutorialScRsp : BasePacket
{
    public PacketUnlockTutorialScRsp(uint tutorialId) : base(CmdIds.UnlockTutorialScRsp)
    {
        var proto = new UnlockTutorialScRsp
        {
            Tutorial = new Proto.Tutorial
            {
                Id = tutorialId,
                Status = TutorialStatus.TutorialUnlock
            }
        };
        SetData(proto);
    }
}