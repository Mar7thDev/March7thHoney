using CastoriceHoney.Server.Data.Excel;
using CastoriceHoney.Server.GameServer.Game.Battle;

namespace CastoriceHoney.Server.GameServer.Game.RogueTourn.Titan;

public class RogueTitanBlessInstance
{
    public List<RogueTournTitanBlessExcel> BlessTypeExcel { get; } = [];

    public List<RogueTournTitanBlessExcel> EnhanceBlessList { get; } = [];

    public void OnBattleStart(BattleInstance inst)
    {
        foreach (var bless in BlessTypeExcel)
            inst.Buffs.Add(new MazeBuff(bless.MazeBuffID, 1, -1)
            {
                WaveFlag = -1
            });

        foreach (var bless in EnhanceBlessList)
            inst.Buffs.Add(new MazeBuff(bless.MazeBuffID, 1, -1)
            {
                WaveFlag = -1
            });
    }
}