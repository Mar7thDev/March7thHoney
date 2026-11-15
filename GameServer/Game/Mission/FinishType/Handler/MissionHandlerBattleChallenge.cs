using March7thHoney.Data.Config;
using March7thHoney.Data.Excel;
using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.Battle;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.Mission.FinishType.Handler;

[MissionFinishType(MissionFinishTypeEnum.BattleChallenge)]
public class MissionHandlerBattleChallenge : MissionFinishTypeHandler
{
    public override async ValueTask HandleMissionFinishType(PlayerInstance player, SubMissionInfo info, object? arg)
    {
        // this type wont be used in mission
        await ValueTask.CompletedTask;
    }

    public override async ValueTask HandleQuestFinishType(PlayerInstance player, QuestDataExcel quest,
        FinishWayExcel excel, object? arg)
    {
        if (arg is BattleInstance instance)
        {
            var progress = 0;
            if (instance.BattleResult == null) return;
            var reported = new HashSet<int>();
            foreach (var battleTargetList in instance.BattleResult.Stt.BattleTargetInfo.Values)
            foreach (var battleTarget in battleTargetList.BattleTargetList_)
            {
                reported.Add((int)battleTarget.Id);
                if (excel.ParamIntList.Contains((int)battleTarget.Id))
                    progress += (int)battleTarget.Progress;
            }

            // The 4.6+ client no longer reports these targets, so read them off the won battle's statistics.
            if (instance.BattleResult.EndStatus == BattleEndStatus.BattleEndWin)
                foreach (var target in excel.ParamIntList.Where(id => !reported.Contains(id)))
                    progress += target switch
                    {
                        20063 or 20072 or 20074 => instance.BattleResult.Stt.MonsterList.Count,
                        30047 => instance.BattleResult.Stt.UltraCnt > 0 ? 1 : 0,
                        _ => 0
                    };

            if (progress > 0 || reported.Count > 0)
                await player.QuestManager!.UpdateQuestProgress(quest.QuestID, progress);
        }
    }
}
