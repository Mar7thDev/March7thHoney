using Google.Protobuf;
using March7thHoney.Data;
using March7thHoney.GameServer.Game.GridFight;
using March7thHoney.Proto;

static class ElationUiRegression
{
    public static void Run(Func<uint?, GridFightManager> newRun, GridFightResourceCatalog catalog,
        Action<string, bool> check)
    {
        if (catalog.CurrentSeason != 2) return;
        var manager = newRun(101);
        var run = manager.Session!;
        run.Roles.Clear();
        var members = catalog.GetRolesByTrait(GridFightSession.ElationTraitId).Take(3).ToList();
        foreach (var member in members)
        {
            run.TryAddRole(member.ID, 1, out _);
            run.Roles.Last().Position = (uint)run.Roles.Count;
        }

        HDCJJFDLHIO WireSlots()
        {
            var info = GridFightGameTraitInfo.Parser.ParseFrom(manager.BuildGameTraitInfo().ToByteArray());
            return info.GridFightTraitInfo.Single(trait => trait.TraitId == GridFightSession.ElationTraitId)
                .TraitEffectList.Single(effect => effect.EffectId == GridFightSession.ElationEffectId).IFDICIIOHHI;
        }

        var beforeInventory = run.Equipments.Count;
        var initial = WireSlots().POJGKJENENA;
        check("Elation Aha: three preset slots carry current equipment config IDs on initial sync",
            initial.Count == 3 && initial.Select(slot => slot.ACAPGLCONJH)
                .SequenceEqual(run.ElationSlots.Select(slot => slot.PresetEquipmentId)) &&
            initial.All(slot => GameData.GridFightEquipmentData.ContainsKey(slot.ACAPGLCONJH)));
        check("Elation Aha: unlock flags follow the active bond without losing locked-slot equipment",
            initial.Count(slot => slot.IsUnlock) == run.ResolveElationUnlockedSlots() &&
            initial.All(slot => slot.ACAPGLCONJH != 0));
        check("Elation Aha: viewing presets does not grant inventory items", run.Equipments.Count == beforeInventory);

        var preset = run.ElationSlots[0].PresetEquipmentId;
        run.TryAddEquipment(preset, out var material);
        var craft = run.DressElationEquipment(material.UniqueId, out var consumed);
        var advanced = craft.AddedEquipments.SingleOrDefault();
        check("Elation Aha: preset and dragged basic equipment craft one advanced item",
            craft.Success && consumed?.UniqueId == material.UniqueId && advanced != null &&
            run.Equipments.All(item => item.UniqueId != material.UniqueId));
        if (advanced == null) return;
        var craftedSlot = WireSlots().POJGKJENENA[0];
        check("Elation Aha: crafted slot sends config ID instead of inventory unique ID",
            craftedSlot.NHLBAOGCIAN == preset && craftedSlot.ACAPGLCONJH == advanced.EquipmentId &&
            craftedSlot.ACAPGLCONJH != advanced.UniqueId);
        var battleEquipment = run.GetElationBattleEquipment();
        check("Elation Aha: displayed advanced item matches the battle item",
            battleEquipment[0].EquipmentId == craftedSlot.ACAPGLCONJH);
        var restored = SnapshotProbe.RoundTrip(catalog, run);
        check("Elation Aha: crafting survives restore", restored.ElationSlots[0].EquipmentUniqueId == advanced.UniqueId &&
            restored.GetElationBattleEquipment()[0].EquipmentId == advanced.EquipmentId);
    }
}
