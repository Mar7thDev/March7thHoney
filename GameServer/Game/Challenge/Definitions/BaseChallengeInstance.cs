using CastoriceHoney.Server.Data.Excel;
using CastoriceHoney.Server.GameServer.Game.Battle;
using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Proto;
using CastoriceHoney.Server.Proto.ServerSide;

namespace CastoriceHoney.Server.GameServer.Game.Challenge.Definitions;

public abstract class BaseChallengeInstance(PlayerInstance player, ChallengeDataPb data)
{
    public PlayerInstance Player { get; } = player;
    public ChallengeDataPb Data { get; } = data;

    public virtual void OnBattleStart(BattleInstance battle)
    {
        battle.OnBattleEnd += OnBattleEnd;
    }

    public virtual async ValueTask OnBattleEnd(BattleInstance battle, PVEBattleResultCsReq req)
    {
        await ValueTask.CompletedTask;
    }

    public abstract Dictionary<int, List<ChallengeConfigExcel.ChallengeMonsterInfo>> GetStageMonsters();

    public virtual void OnUpdate()
    {
    }
}