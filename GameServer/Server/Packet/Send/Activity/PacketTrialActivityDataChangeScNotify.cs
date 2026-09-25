using March7thHoney.Database.Activity;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Activity;

public class PacketTrialActivityDataChangeScNotify : BasePacket
{
    public PacketTrialActivityDataChangeScNotify(TrialActivityResultData record, IEnumerable<PAALDKHKOLE> avatars)
        : base(CmdIds.TrialActivityDataChangeScNotify)
    {
        var proto = new TrialActivityDataChangeScNotify
        {
            TrialActivityInfo = new TrialActivityInfo
            {
                StageId = (uint)record.StageId,
                TakenReward = record.TakenReward
            }
        };
        proto.OBOPMFBGBAE.Add(avatars);

        SetData(proto);
    }
}
