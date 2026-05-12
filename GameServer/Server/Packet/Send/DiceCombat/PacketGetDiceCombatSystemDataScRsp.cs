using March7thHoney.GameServer.Game.Activity.Activities.DiceCombat;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.DiceCombat;

public class PacketGetDiceCombatSystemDataScRsp : BasePacket
{
    public PacketGetDiceCombatSystemDataScRsp(PlayerInstance player) : base(CmdIds.GetDiceCombatSystemDataScRsp)
    {
        var instance = player.ActivityManager!.DiceCombat;
        var proto = new GetSystemDataRsp { Retcode = 0 };

        foreach (var (avatarId, state) in instance.Avatars)
            proto.AvatarList.Add(instance.BuildAvatarProto(avatarId, state));

        foreach (var id in instance.FeatureUnlocks) proto.FKOBFPNGDCG.Add(id);

        foreach (var (stageId, state) in instance.Stages)
        {
            if (!state.Completed) continue;
            proto.FAMFCIPLGON.Add(new INACEGAAALJ
            {
                FLEOCMEFANH = stageId,
                UnlockTimestamp = state.CompletedTime
            });
        }

        var scoreState = new DABNCEPBKMG();
        scoreState.IOJMFDDPBHI.AddRange(instance.ScoreTakenIds);
        foreach (var (stageId, damage) in DiceCombatConfig.StageDamageRecord)
            scoreState.JFEKKNNHCOH.Add(new MDFEFKDEFML { StageId = stageId, BPNBNPGPFJK = damage });
        proto.DKCGALDKDFA = scoreState;

        SetData(proto);
    }
}
