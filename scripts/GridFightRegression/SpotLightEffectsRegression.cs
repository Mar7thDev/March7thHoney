using System.Reflection;
using Google.Protobuf;
using March7thHoney.Data;
using March7thHoney.Enums.GridFight;
using March7thHoney.GameServer.Game.GridFight;
using March7thHoney.GameServer.Game.GridFight.Battle;
using March7thHoney.Proto;

static class SpotLightEffectsRegression
{
    static List<GridFightSpotLightState> Crowns(GridFightSession run) => (List<GridFightSpotLightState>)typeof(GridFightSession)
        .GetField("_spotLights", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(run)!;
    static void Call(GridFightSession run, string name) => typeof(GridFightSession)
        .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(run, null);

    public static void Run(Func<uint?, GridFightManager> newRun, GridFightResourceCatalog catalog, Action<string, bool> check)
    {
        foreach (var (id, strength) in new[] { (100100u, 15u), (100106u, 20u) })
        {
            var manager = newRun(101);
            var run = manager.Session!;
            var role = run.Roles.First();
            foreach (var unit in run.Roles) unit.Position = 0;
            role.Position = 1;
            Crowns(run).Add(new GridFightSpotLightState { UniqueId = 9100, SpotLightId = id, RoleId = role.RoleId, RoleUniqueId = role.UniqueId });
            run.AdjustGold(100);
            var before = run.FreeRefreshCount;
            Call(run, "RecordSpotLightBattleParticipants");
            role.Position = 0; // Eligibility is last battle participation, not current placement.
            Call(run, "OnSpotLightNodeEntered");
            check($"Refresh Vision {id}: participant grants one free refresh", run.FreeRefreshCount == before + 1);
            Call(run, "OnSpotLightNodeEntered");
            check($"Refresh Vision {id}: duplicate node callback grants nothing", run.FreeRefreshCount == before + 1);
            check($"Refresh Vision {id}: free and paid refreshes both stack", run.RefreshShop(true) && run.RefreshShop(true) &&
                run.SpotLightStrengthBonusFixpoint == strength * 2);
            check($"Refresh Vision {id}: automatic shop population adds no stack", run.RefreshShop(false) && run.SpotLightStrengthBonusFixpoint == strength * 2);
            var properties = new Dictionary<uint, uint>();
            run.ApplyInvestmentPropertyScales(properties);
            check($"Refresh Vision {id}: both combat strength properties grow", properties[1027] == strength * 2 && properties[1030] == strength * 2);
            var restored = SnapshotProbe.RoundTrip(catalog, run);
            check($"Refresh Vision {id}: stacks persist", restored.SpotLightStrengthBonusFixpoint == strength * 2);
            Crowns(run).Single().IsLoss = true;
            run.RefreshShop(true);
            check($"Refresh Vision {id}: lost crown stops applying strength", run.SpotLightStrengthBonusFixpoint == 0);
        }

        var nano = newRun(101).Session!;
        var wearer = nano.Roles.First();
        Crowns(nano).Add(new GridFightSpotLightState { UniqueId = 9200, SpotLightId = 100101, RoleId = wearer.RoleId, RoleUniqueId = wearer.UniqueId });
        var upgrade = GameData.GridFightEquipUpgradeData.Values.First(row =>
            GameData.GridFightEquipmentData.GetValueOrDefault(row.PreID)?.EquipCategory == GridFightEquipCategoryEnum.Craftable &&
            GameData.GridFightEquipmentData.GetValueOrDefault(row.UpgradeID)?.EquipCategory == GridFightEquipCategoryEnum.Radiant);
        nano.TryAddEquipment(upgrade.PreID, out var first);
        nano.TryAddEquipment(upgrade.PreID, out var second);
        wearer.EquippedEquipmentUniqueIds.Clear();
        wearer.EquippedEquipmentUniqueIds.AddRange([first!.UniqueId, second!.UniqueId]);
        var battleItems = nano.GetBattleEquipment(wearer);
        check("Nano Modification: exactly one advanced item upgrades", battleItems.Count(item => item.EquipmentId == upgrade.UpgradeID) == 1 &&
            battleItems.Count(item => item.EquipmentId == upgrade.PreID) == 1);
        check("Nano Modification: preparation equipment stays unchanged", first.EquipmentId == upgrade.PreID && second.EquipmentId == upgrade.PreID);
        check("Nano Modification: repeated battle construction is stable", nano.GetBattleEquipment(wearer).SequenceEqual(battleItems));
        var packet = GridFightBattleProtoBuilder.BuildRoleInfo(nano, wearer, Gender.Man);
        check("Nano Modification: battle packet contains the upgraded item", packet.RoleEquipmentList.Count(item =>
            item.GridFightEquipmentId == upgrade.UpgradeID) == 1);
        var reloaded = SnapshotProbe.RoundTrip(catalog, nano);
        check("Nano Modification: reloading retains original inventory", reloaded.Equipments.Single(item => item.UniqueId == first.UniqueId).EquipmentId == upgrade.PreID);
        var other = nano.Roles.Skip(1).First();
        other.EquippedEquipmentUniqueIds.Add(first.UniqueId);
        check("Nano Modification: affects only the crowned role", nano.GetBattleEquipment(other).Single().EquipmentId == upgrade.PreID);
        wearer.EquippedEquipmentUniqueIds.Clear();
        check("Nano Modification: no eligible gear creates no item", nano.GetBattleEquipment(wearer).Count == 0);
    }
}
