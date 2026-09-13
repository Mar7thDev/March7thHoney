using March7thHoney.Data.Custom;

namespace March7thHoney.GameServer.Game.Rogue;

public class RogueBuffSelectMenu
{
    private const int OfferCount = 3;

    public List<BaseRogueBuffExcel> SelectBuffs { get; private set; } = [];
    public bool HasSelection => SelectBuffs.Count > 0;

    public static RogueBuffSelectMenu Create(RogueInstance inst, List<BaseRogueBuffExcel> pool)
    {
        return new RogueBuffSelectMenu
        {
            SelectBuffs = pool.Where(x => !inst.Buffs.ContainsKey(x.MazeBuffID))
                .OrderBy(_ => Random.Shared.Next())
                .Take(OfferCount)
                .OrderBy(x => x.MazeBuffID)
                .ToList()
        };
    }

    public BaseRogueBuffExcel? Select(RogueInstance inst, int buffId)
    {
        var chosen = SelectBuffs.FirstOrDefault(x => x.MazeBuffID == buffId);
        if (chosen == null) return null;

        inst.AddBuff(chosen);
        SelectBuffs = [];
        return chosen;
    }
}
