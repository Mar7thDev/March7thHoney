using March7thHoney.Data;
using March7thHoney.GameServer.Game.Battle;

namespace March7thHoney.GameServer.Game.GridFight;

public sealed partial class GridFightSession
{
    public const uint BlackHoleGravitySpotLightId = 100900;
    public const int BlackHoleGravityBuffId = 131000002;

    /// <summary>
    /// S2 effect buffs live in GridFightTraitMazebuffS2/GridFightSpotLightMazebuff, not MazeBuff.json.
    /// Their StageAbilityBeforeCharacterBorn binding uses the client's own parameters and callbacks.
    /// </summary>
    public IReadOnlyList<MazeBuff> GetSeasonTwoBattleEffectBuffs()
    {
        if (!IsSeasonTwoRules) return [];
        var buffs = new List<MazeBuff>();
        if (ResolveTraitLayer(ClaraTraitId) > 0)
        {
            var owned = _claraEnhancements.Where(entry => entry.Num > 0).Select(entry => entry.EnhanceId).ToHashSet();
            // These upgrades replace the same modifier; do not let the earlier tier overwrite its successor.
            foreach (var chain in new uint[][] { [1001, 1002, 1003], [2001, 2002, 2003], [3001, 3002, 3003, 3004] })
            {
                var strongest = chain.LastOrDefault(owned.Contains);
                owned.ExceptWith(chain.Where(id => id != strongest));
            }
            foreach (var row in GridFightSeasonTwoTables.ClaraEnhance.Where(row => owned.Contains(row.Id) && row.BuffId > 0))
                Add((int)row.BuffId);
        }
        foreach (var role in Roles.Where(role => IsActivePosition(role.Position) &&
                     GetSpotLightIdForRole(role.UniqueId) == BlackHoleGravitySpotLightId))
        {
            if (Add(BlackHoleGravityBuffId) is not { } buff) continue;
            // The stage ability reads this exact key (legacy string hash 1535408766), then matches RoleID.
            buff.DynamicValues["role_id"] = role.RoleId;
            buff.DynamicValues["#ADF_1"] = 10;
            buff.DynamicValues["#ADF_2"] = 3;
        }
        return buffs;

        MazeBuff? Add(int id)
        {
            if (!GameData.MazeBuffData.ContainsKey(id * 10 + 1)) return null;
            var buff = new MazeBuff(id, 1, -1) { WaveFlag = -1, IncludeOwnerInTargetList = false };
            buffs.Add(buff);
            return buff;
        }
    }
}
