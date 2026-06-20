using March7thHoney.Enums.Scene;
using March7thHoney.Kcp;
using March7thHoney.Proto;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Server.Packet.Recv.ChallengePeak;

[Opcode(CmdIds.LeaveChallengePeakCsReq)]
public class HandlerLeaveChallengePeakCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        // Only run the leave cleanup (heal team, teleport) when actually inside a challenge-type
        // scene — otherwise a client could spam this for a free full heal + teleport.
        if (player.SceneInstance is not null && player.SceneInstance.Excel.PlaneType == PlaneTypeEnum.Challenge)
        {
            player.LineupManager!.SetExtraLineup(ExtraLineupType.LineupChallenge, []);


            player.ChallengeManager!.ChallengeInstance = null;
            player.ChallengeManager!.ClearInstance();

            player.ChallengePeakManager!.BossIsHard = false;

            // Leave scene
            player.LineupManager!.SetExtraLineup(ExtraLineupType.LineupNone, []);
            // Heal avatars (temproary solution)
            foreach (var avatar in player.LineupManager.GetCurLineup()!.AvatarData!.FormalAvatars)
                avatar.CurrentHp = 10000;

            var leaveEntryId = GameConstants.CHALLENGE_PEAK_ENTRANCE;
            if (player.SceneInstance.LeaveEntryId != 0) leaveEntryId = player.SceneInstance.LeaveEntryId;
            await player.EnterScene(leaveEntryId, 0, true);
        }

        await connection.SendPacket(CmdIds.LeaveChallengePeakScRsp);
    }
}
