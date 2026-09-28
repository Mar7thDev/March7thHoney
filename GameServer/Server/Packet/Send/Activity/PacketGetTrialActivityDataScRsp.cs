using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Activity;

public class PacketGetTrialActivityDataScRsp : BasePacket
{
    public PacketGetTrialActivityDataScRsp(PlayerInstance player) : base(CmdIds.GetTrialActivityDataScRsp)
    {
        var manager = player.ActivityManager!;
        var proto = new GetTrialActivityDataScRsp
        {
            // The running trial stage; 0 when none is running.
            ActivityStageId = (uint)manager.Data.TrialActivityData.CurTrialStageId
        };
        proto.TrialActivityInfoList.Add(manager.Data.TrialActivityData.ToProto());
        proto.OBOPMFBGBAE.Add(manager.GetTrialAvatarProtoList());
        SetData(proto);
    }
}
