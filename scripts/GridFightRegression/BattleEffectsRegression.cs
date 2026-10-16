using System.Reflection;
using March7thHoney.Data;
using March7thHoney.GameServer.Game.GridFight;

static class BattleEffectsRegression
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void Run(Func<uint?, GridFightManager> newRun, GridFightResourceCatalog catalog,
        Action<string, bool> check)
    {
        if (catalog.CurrentSeason != 2) return;
        check("S2 separate MazeBuff tables: black hole is registered with its stage ability",
            GameData.MazeBuffData.TryGetValue(GridFightSession.BlackHoleGravityBuffId * 10 + 1, out var gravity) &&
            gravity.InBattleBindingKey == "StageAbility_GridFight_SpotLight_131000002");
        check("S2 separate MazeBuff tables: every Clara module buff is available", GridFightSeasonTwoTables.ClaraEnhance
            .Where(row => row.BuffId > 0).All(row => GameData.MazeBuffData.ContainsKey((int)row.BuffId * 10 + 1)));
        check("S2 separate MazeBuff tables: train upgrades are available", Enumerable.Range(110000021, 7)
            .All(id => GameData.MazeBuffData.ContainsKey(id * 10 + 1)));

        var manager = newRun(101);
        var run = manager.Session!;
        run.Roles.Clear();
        run.TryAddRole(GridFightSession.ClaraRoleId, 1, out _);
        var clara = run.Roles.Single();
        clara.Position = 1;
        var enhancements = (List<(uint EnhanceId, uint Num)>)typeof(GridFightSession)
            .GetField("_claraEnhancements", Hidden)!.GetValue(run)!;
        foreach (var row in GridFightSeasonTwoTables.ClaraEnhance.Where(row => row.BuffId > 0))
        {
            enhancements.Clear();
            enhancements.Add((row.Id, 1));
            var buffs = run.GetSeasonTwoBattleEffectBuffs();
            check($"Clara module {row.Id}: emits its real stage ability buff", buffs.Count == 1 && buffs[0].BuffID == row.BuffId &&
                buffs[0].BuffLevel == 1 && !buffs[0].IncludeOwnerInTargetList);
        }
        enhancements.Clear();
        enhancements.AddRange(new (uint, uint)[] { (1001, 1), (1002, 1), (1003, 1), (3001, 1), (3002, 1), (3004, 1) });
        var latest = run.GetSeasonTwoBattleEffectBuffs().Select(buff => buff.BuffID).Order().ToList();
        check("Clara upgraded modules replace older tiers", latest.SequenceEqual(new[] { 110000033, 110000046 }));
        clara.Position = 14;
        check("Clara bench does not activate module effects", run.GetSeasonTwoBattleEffectBuffs().Count == 0);
        clara.Position = 1;
        var restored = SnapshotProbe.RoundTrip(catalog, run);
        check("Clara module battle effects survive restore", restored.GetSeasonTwoBattleEffectBuffs().Select(buff => buff.BuffID)
            .Order().SequenceEqual(latest));

        manager = newRun(101);
        run = manager.Session!;
        run.Roles.Clear();
        run.TryAddRole(catalog.RolePool[0].ID, 1, out _);
        var role = run.Roles.Single();
        role.Position = 1;
        var lights = (List<GridFightSpotLightState>)typeof(GridFightSession).GetField("_spotLights", Hidden)!.GetValue(run)!;
        var light = new GridFightSpotLightState { UniqueId = 9000, RoleId = role.RoleId, RoleUniqueId = role.UniqueId,
            SpotLightId = GridFightSession.BlackHoleGravitySpotLightId };
        lights.Add(light);
        var buff = run.GetSeasonTwoBattleEffectBuffs().Single();
        check("black hole: binds the selected role, 10 AV and 3 triggers", buff.BuffID == GridFightSession.BlackHoleGravityBuffId &&
            buff.DynamicValues["role_id"] == role.RoleId && buff.DynamicValues["#ADF_1"] == 10 && buff.DynamicValues["#ADF_2"] == 3);
        restored = SnapshotProbe.RoundTrip(catalog, run);
        check("black hole: binding survives restore", restored.GetSeasonTwoBattleEffectBuffs().Single().DynamicValues["role_id"] == role.RoleId);
        light.IsLoss = true;
        check("black hole: lost spotlight has no buff", run.GetSeasonTwoBattleEffectBuffs().Count == 0);
        light.IsLoss = false;
        role.Position = 14;
        check("black hole: bench role has no battle buff", run.GetSeasonTwoBattleEffectBuffs().Count == 0);
        run.Roles.Clear();
        check("black hole: sold role cannot leave a battle effect", run.GetSeasonTwoBattleEffectBuffs().Count == 0);
    }
}
