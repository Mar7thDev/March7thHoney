using System.Reflection;
using Google.Protobuf;
using March7thHoney.GameServer.Game.GridFight;
using March7thHoney.Proto;

static class OwlbertRegression
{
    static object? Call(GridFightSession run, string name, params object[] args) => typeof(GridFightSession)
        .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(run, args);

    public static void Run(Func<uint?, GridFightManager> newRun, GridFightResourceCatalog catalog,
        Action<string, bool> check)
    {
        foreach (var (augment, npcId) in new[] { (303201u, 901u), (303301u, 902u), (332401u, 903u) })
        {
            var manager = newRun(101);
            var run = manager.Session!;
            run.Roles.Clear();
            var gold = run.Gold;
            var equipmentCount = run.Equipments.Count;
            run.ActiveInvestmentIds.Add(augment);
            Call(run, "ApplyAugmentSelectionEffects", augment);
            var npc = run.OwlbertNpcs.Single();
            check($"Owlbert {augment}: creates the right NPC in a free back slot", npc.NpcId == npcId && npc.Position is >= 5 and <= 13);
            var rewardItems = run.Equipments.Skip(equipmentCount).Where(item => !item.IsLocked).ToList();
            check($"Owlbert {augment}: grants its opening reward", augment switch
            {
                303201 => run.Gold == gold,
                303301 => rewardItems.Count == 9 && rewardItems.Select(item => item.EquipmentId).Distinct().Count() == 1,
                _ => rewardItems.Count == 9 && rewardItems.All(item => item.EquipmentId == 352201),
            });
            check($"Owlbert {augment}: display equipment remains locked on the NPC",
                npc.EquippedEquipmentUniqueIds.Count == (augment == 303201 ? 0 : 3) &&
                npc.EquippedEquipmentUniqueIds.All(id => run.Equipments.Single(item => item.UniqueId == id).IsLocked));
            var sync = GridFightSyncUpdateResultScNotify.Parser.ParseFrom(manager.BuildInvestmentSelectionSync(0, augment).ToByteArray());
            check($"Owlbert {augment}: selection emits NPC arrival", sync.ResultList.SelectMany(result => result.UpdateDynamicList)
                .Any(update => update.EJMDHDJCGDI?.Id == npcId));
            var current = manager.BuildCurrentInfo()!;
            check($"Owlbert {augment}: reconnect snapshot includes NPC", current.GameInfoList
                .Single(info => info.TeamInfo != null).TeamInfo.GridGameNpcList.Any(item => item.Id == npcId));
            var count = run.Equipments.Count;
            gold = run.Gold;
            Call(run, "InitializeOwlbert", augment);
            check($"Owlbert {augment}: duplicate initialization gives no second reward", run.Equipments.Count == count && run.Gold == gold && run.OwlbertNpcs.Count == 1);
            var restored = SnapshotProbe.RoundTrip(catalog, run);
            check($"Owlbert {augment}: NPC and locked equipment survive restore", restored.OwlbertNpcs.Single().UniqueId == npc.UniqueId &&
                restored.OwlbertNpcs.Single().EquippedEquipmentUniqueIds.SequenceEqual(npc.EquippedEquipmentUniqueIds));
            Call(restored, "OnAugmentEffectNodeEntered", restored.CurrentNode);
            check($"Owlbert {augment}: item rewards are not repeated on new nodes", restored.Gold == gold && restored.Equipments.Count == count);
            var clock = typeof(GridFightSession).GetField("_goldOwlbertTickMs", BindingFlags.NonPublic | BindingFlags.Instance)!;
            clock.SetValue(restored, 1000L);
            check($"Owlbert {augment}: no premature or unrelated timed payout", !restored.AccrueGoldOwlbert(1499));
            var changed = restored.AccrueGoldOwlbert(2000);
            check($"Owlbert {augment}: gold grants one coin every half second", changed == (augment == 303201) &&
                restored.Gold - gold == (augment == 303201 ? 2u : 0u));
            if (augment == 303201)
            {
                restored.AccrueGoldOwlbert(201000);
                check("Gold Owlbert: per-node cap is 200", restored.Gold - gold == 200 && !restored.AccrueGoldOwlbert(301000));
                var saved = SnapshotProbe.RoundTrip(catalog, restored);
                check("Gold Owlbert: reload cannot claim the same node again", !saved.AccrueGoldOwlbert(long.MaxValue));
                Call(saved, "OnAugmentEffectNodeEntered", saved.CurrentNode);
                clock.SetValue(saved, 1000L);
                check("Gold Owlbert: a new node resets the cap", saved.AccrueGoldOwlbert(1500) && saved.Gold - gold == 201);
            }
            var legacy = newRun(101).Session!;
            legacy.ActiveInvestmentIds.Add(augment);
            var recovered = SnapshotProbe.RoundTrip(catalog, legacy);
            var recoveredAgain = SnapshotProbe.RoundTrip(catalog, recovered);
            check($"Owlbert {augment}: old saves recover once without duplicate rewards", recovered.OwlbertNpcs.Count == 1 &&
                recoveredAgain.Equipments.Count == recovered.Equipments.Count && recoveredAgain.OwlbertNpcs.Count == 1);
        }
    }
}
