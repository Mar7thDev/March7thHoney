using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Activity;

public class PacketTrialActivityDataChangeScNotify : BasePacket
{
    public PacketTrialActivityDataChangeScNotify(uint stageId) : base(CmdIds.TrialActivityDataChangeScNotify)
    {
        var proto = new TrialActivityDataChangeScNotify
        {
            TrialActivityInfo =
            {
                StageId = stageId
            }
        };

        SetData(proto);
    }
}