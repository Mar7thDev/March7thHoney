using March7thHoney.Data;
using March7thHoney.Data.Config;
using March7thHoney.Database.Inventory;
using March7thHoney.Enums.Scene;
using March7thHoney.GameServer.Game.Drop;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.Scene;
using March7thHoney.GameServer.Game.Scene.Entity;
using March7thHoney.GameServer.Game.Task.AvatarTask;
using March7thHoney.GameServer.Server.Packet.Send.Scene;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.Battle;

public class SceneSkillManager(PlayerInstance player) : BasePlayerManager(player)
{
    private const int SpeedDestructEventId = 10003;
    private const int SpeedDestructBuffId = 2041101;
    private const int SpeedDestructBuffDurationSec = 15;

    public async ValueTask<SkillResultData> OnCast(SceneCastSkillCsReq req)
    {
        var handledDestruct = await HandleDestructPropDrops(req);

        // get entities
        List<BaseGameEntity> targetEntities = []; // enemy
        BaseGameEntity? attackEntity; // caster
        List<int> addEntityIds = [];
        foreach (var id in req.AssistMonsterEntityIdList)
            if (Player.SceneInstance!.Entities.TryGetValue((int)id, out var v))
            {
                targetEntities.Add(v);
                addEntityIds.Add((int)id);
            }

        foreach (var info in req.AssistMonsterEntityInfo)
        foreach (var id in info.EntityIdList)
        {
            if (addEntityIds.Contains((int)id)) continue;
            if (Player.SceneInstance!.Entities.TryGetValue((int)id, out var v))
            {
                targetEntities.Add(v);
                addEntityIds.Add((int)id);
            }
        }

        attackEntity = Player.SceneInstance!.Entities.GetValueOrDefault((int)req.AttackedByEntityId);
        if (attackEntity == null)
            return handledDestruct
                ? new SkillResultData(Retcode.RetSucc)
                : new SkillResultData(Retcode.RetSceneEntityNotExist);
        // get ability file
        var abilities = GetAbilityConfig(attackEntity);

        // Fallback path for avatars whose ConfigAdventureAbility/LocalPlayer/<Name>_00_Ability.json
        // hasn't been dumped from the client yet (e.g. brand-new 4.3 characters before dim picks them up).
        // We can't run the ability's OnStart task chain without that file, but the cast itself can still
        // resolve: trigger battle when assist monsters were reported by the client, and let the
        // downstream maze-buff backfill (below) bind the avatar's SkillMaze maze buffs. No avatar / monster
        // id is hard-coded — selection is purely data-driven from req.AssistMonster* and entity types.
        AbilityLevelResult res;
        if (abilities == null || abilities.AbilityList.Count < 1)
        {
            res = await BuildAbilitylessCast(attackEntity, targetEntities, req);
        }
        else
        {
            var abilityName = !string.IsNullOrEmpty(req.MazeAbilityStr) ? req.MazeAbilityStr :
                req.SkillIndex == 0 ? "NormalAtk01" : "MazeSkill";
            var targetAbility = abilities.AbilityList.Find(x => x.Name.Contains(abilityName));
            if (targetAbility == null)
            {
                targetAbility = abilities.AbilityList.FirstOrDefault();
                if (targetAbility == null)
                    return new SkillResultData(Retcode.RetMazeNoAbility);
            }

            // execute ability
            res = await Player.TaskManager!.AbilityLevelTask.TriggerTasks(abilities, targetAbility.OnStart,
                attackEntity, targetEntities, req);
        }

        // check if avatar execute
        if (attackEntity is AvatarSceneInfo) await Player.SceneInstance!.OnUseSkill(req);

        // backfill character maze buffs for techniques whose ability JSON spawns
        // a summon unit instead of calling AddMazeBuff (e.g. Dahlia 1321 -> 132101,
        // Silver Wolf lv.999 1506 -> 150601 which uses CharacterSkillAdv binding).
        // candidates = AvatarMazeBuff entries bound to the SkillMaze slot via either
        // CharacterSkill or CharacterSkillAdv. If the ability JSON already added one
        // of them, skip entirely; otherwise apply the canonical primary entry
        // (UseType SummonUnit/AddBattleBuff/TriggerBattle) so battleInfo.buffList
        // picks it up. This keeps multi-entry avatars (Cyrene, Cerydra, Robin...)
        // from receiving stray summon/listener buffs.
        if (req.SkillIndex == 1 && attackEntity is AvatarSceneInfo casterAvatar)
        {
            var avatarId = casterAvatar.AvatarInfo.BaseAvatarId;
            var candidates = GameData.MazeBuffData
                .Where(kvp => kvp.Key / 10 / 100 == avatarId
                              && kvp.Value.MazeBuffType == "Character"
                              && (kvp.Value.InBattleBindingType == "CharacterSkill"
                                  || kvp.Value.InBattleBindingType == "CharacterSkillAdv")
                              && kvp.Value.InBattleBindingKey == "SkillMaze")
                .Select(kvp => (BuffId: kvp.Key / 10, Excel: kvp.Value))
                .ToList();

            var alreadyApplied = candidates.Any(c =>
                casterAvatar.BuffList.Any(b => b.BuffId == c.BuffId));

            if (!alreadyApplied)
            {
                var primary = candidates.FirstOrDefault(c =>
                    c.Excel.UseType is "SummonUnit" or "AddBattleBuff" or "TriggerBattle");
                if (primary.BuffId != 0)
                    await casterAvatar.AddBuff(new SceneBuff(primary.BuffId, primary.Excel.Lv, avatarId));
            }
        }

        return new SkillResultData(Retcode.RetSucc, res.Instance, res.BattleInfos);
    }

    private async ValueTask<bool> HandleDestructPropDrops(SceneCastSkillCsReq req)
    {
        if (Player.SceneInstance == null || Player.InventoryManager == null) return false;

        var handled = false;

        HashSet<int> hitEntityIds = [(int)req.AttackedByEntityId];
        foreach (var id in req.AssistMonsterEntityIdList)
            hitEntityIds.Add((int)id);

        foreach (var info in req.AssistMonsterEntityInfo)
        foreach (var id in info.EntityIdList)
            hitEntityIds.Add((int)id);

        foreach (var entityId in hitEntityIds)
        {
            if (!Player.SceneInstance.Entities.TryGetValue(entityId, out var entity)) continue;
            if (entity is not EntityProp prop) continue;
            if (prop.Excel.PropType != PropTypeEnum.PROP_DESTRUCT) continue;
            handled = true;

            if (prop.PropInfo.EventID > 0)
            {
                await ApplyPropEventEffect(prop, req);

                var eventItems = BuildPlaneEventRewards(prop.PropInfo.EventID);
                if (eventItems.Count > 0)
                {
                    await Player.InventoryManager.AddItems(eventItems);
                }
                else
                {
                    // Client still expects a plane-event notify for special destructibles with no item reward.
                    await Player.SendPacket(new PacketScenePlaneEventScNotify(new ItemList()));
                }
            }

            if (prop.PropInfo.ChestID > 0)
            {
                var items = DropService.CalculateDropsFromProp(prop.PropInfo.ChestID);
                if (items.Count > 0) await Player.InventoryManager.AddItems(items);
            }

            // Destruct props should disappear from scene instead of being refreshed as opened state.
            await Player.SceneInstance.RemoveEntity(prop, false);
            await Player.SendPacket(new PacketSceneGroupRefreshScNotify(
                Player,
                null,
                prop,
                SceneGroupRefreshType.Afibfmafncc));

            // Persist as opened for non-reset groups so relog won't respawn it.
            if (prop.Group.SaveType != SaveTypeEnum.Reset)
                Player.SetScenePropData(Player.SceneInstance.FloorId, prop.GroupId, prop.PropInfo.ID, PropStateEnum.Open);
        }

        return handled;
    }

    private async ValueTask ApplyPropEventEffect(EntityProp prop, SceneCastSkillCsReq req)
    {
        if (prop.PropInfo.EventID != SpeedDestructEventId) return;
        if (Player.SceneInstance == null) return;

        var casterEntity = Player.SceneInstance.Entities.GetValueOrDefault((int)req.AttackedByEntityId);
        if (casterEntity is not AvatarSceneInfo casterAvatar) return;

        await casterAvatar.AddBuff(new SceneBuff(
            SpeedDestructBuffId,
            1,
            casterAvatar.AvatarInfo.BaseAvatarId,
            SpeedDestructBuffDurationSec));
    }

    private List<ItemData> BuildPlaneEventRewards(int eventId)
    {
        List<ItemData> items = [];
        GameData.PlaneEventData.TryGetValue(eventId * 10 + Player.Data.WorldLevel, out var planeEvent);
        if (planeEvent == null) return items;

        AddRewardItems(planeEvent.Reward, items);
        foreach (var dropRewardId in planeEvent.DropList) AddRewardItems(dropRewardId, items);

        return items;
    }

    private static void AddRewardItems(int rewardId, List<ItemData> items)
    {
        GameData.RewardDataData.TryGetValue(rewardId, out var reward);
        if (reward == null) return;

        if (reward.Hcoin > 0)
            items.Add(new ItemData
            {
                ItemId = 1,
                Count = reward.Hcoin
            });

        foreach (var (itemId, count) in reward.GetItems())
            items.Add(new ItemData
            {
                ItemId = itemId,
                Count = count
            });
    }

    // Fallback for avatars missing an AdventureAbility JSON: mirror the minimal contract that
    // AbilityLevelTask.AdventureTriggerAttack would have produced — open-world battle is triggered
    // when the client reported assist monsters (普攻命中怪), and HitMonsterInstance entries are
    // emitted for every reported entity so the client knows which ones to roll into the battle.
    // No avatar / monster / entity id is hard-coded: selection is purely from req + entity types.
    private async ValueTask<AbilityLevelResult> BuildAbilitylessCast(
        BaseGameEntity attackEntity,
        List<BaseGameEntity> targetEntities,
        SceneCastSkillCsReq req)
    {
        if (targetEntities.Count == 0)
            return new AbilityLevelResult();

        // A projectile/field technique that runs out its lifetime (chains / persistent
        // field, e.g. Gilgamesh) reports SCENE_CAST_SKILL_PROJECTILE_LIFETIME_FINISH and must
        // NOT start an open-world battle: the client predicts a summon, not combat, and
        // rejects an unexpected battle with "技能使用失败". An attack that actually connected
        // (normal atk, or an attack technique like Himeko) reports only ..._HIT and still
        // engages. Splitting on this client tag keeps attack vs summon techniques correct
        // without an ability JSON and without hard-coding any avatar id.
        if (req.SkillExtraTags.Contains(SkillExtraTag.SceneCastSkillProjectileLifetimeFinish))
            return new AbilityLevelResult();

        var battleInfos = new List<HitMonsterInstance>();
        foreach (var entity in targetEntities)
        {
            var type = entity is EntityMonster { IsAlive: false }
                ? MonsterBattleType.DirectDieSkipBattle
                : MonsterBattleType.TriggerBattle;
            battleInfos.Add(new HitMonsterInstance(entity.EntityId, type));
        }

        var hasLiveMonster = targetEntities.OfType<EntityMonster>().Any(x => x.IsAlive);
        BattleInstance? instance = null;
        if (hasLiveMonster)
            instance = await Player.BattleManager!.StartBattle(attackEntity, targetEntities, req.SkillIndex == 1);

        return new AbilityLevelResult(instance, battleInfos);
    }

    private AdventureAbilityConfigListInfo? GetAbilityConfig(BaseGameEntity entity)
    {
        if (entity is EntityMonster monster)
            return GameData.AdventureAbilityConfigListData.GetValueOrDefault(monster.MonsterData.ID);

        if (entity is AvatarSceneInfo avatar)
            if (GameData.AvatarConfigData.TryGetValue(avatar.AvatarInfo.AvatarId, out var excel))
                return GameData.AdventureAbilityConfigListData.GetValueOrDefault(excel.AdventurePlayerID);

        return null;
    }
}

public record SkillResultData(
    Retcode RetCode,
    BattleInstance? Instance = null,
    List<HitMonsterInstance>? TriggerBattleInfos = null);
