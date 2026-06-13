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

        // Mark the caster as having released a technique (秘技) this scene. A technique is cast either
        // via SceneCastSkillCostMp (barrier/summon: Dahlia, Cyrene, Silver Wolf...) OR as a skillIndex-1
        // maze-skill SceneCastSkill (transform/mech techniques like Himeko Nova send NO CostMp). Flagging
        // on skillIndex 1 here, together with the CostMp handler, makes the scene->battle technique relay
        // general across every cast style — gated on a real in-scene cast, never on freesr. This only
        // records intent (HashSet add); the buffs are injected as battle data at StartBattle, so no scene
        // modifier runs and no avatar id is hard-coded.
        if (req.SkillIndex == 1 && attackEntity is AvatarSceneInfo techniqueCaster)
            Player.SceneInstance!.CastTechniqueAvatarIds.Add(techniqueCaster.AvatarInfo.BaseAvatarId);

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

        // Deterministic, res-independent engagement for NORMAL attacks: a basic attack (skillIndex 0)
        // that connected with a live monster always starts a battle, even when the avatar's
        // AdventureAbility JSON is missing or its task chain never calls TriggerBattle — the robust
        // fix for avatars that otherwise stall on the battle-entry screen (e.g. Firefly). Scoped to
        // skillIndex 0: a skill cast (skillIndex 1) can be a field/summon technique (e.g. Dahlia's
        // barrier projectile) that must not force combat, so those keep relying on the ability chain
        // / abilityless guard. No avatar id is hard-coded.
        if (res.Instance == null && req.SkillIndex == 0 &&
            targetEntities.OfType<EntityMonster>().Any(m => m.IsAlive))
        {
            var instance = await Player.BattleManager!.StartBattle(attackEntity, targetEntities, false);
            if (instance != null)
                res = new AbilityLevelResult(instance, BuildHitMonsterInfos(targetEntities));
        }

        // check if avatar execute
        if (attackEntity is AvatarSceneInfo) await Player.SceneInstance!.OnUseSkill(req);

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

        // A normal attack (skillIndex 0) always engages on a live hit. A skill cast (skillIndex 1)
        // only engages when the caster's technique is a battle-triggering ATTACK — i.e. the avatar
        // has a real SkillMaze maze buff (e.g. Firefly's mech, which reports HIT + LIFETIME_FINISH and
        // must engage). A pure field/summon technique (Gilgamesh's chains) has no SkillMaze maze buff
        // and merely places a projectile that reports a lifetime-finish; starting a battle there makes
        // the client reject it with "技能使用失败". Its real engagement is a separate skillIndex-0
        // attack. The two casts are identical on the wire (same skillIndex + tags + monster hit), so
        // we lean on the maze-buff data the official client encodes in its ability config. No avatar
        // id is hard-coded.
        if (req.SkillIndex == 1
            && req.SkillExtraTags.Contains(SkillExtraTag.SceneCastSkillProjectileLifetimeFinish)
            && !HasBattleTriggerTechnique((attackEntity as AvatarSceneInfo)?.AvatarInfo.BaseAvatarId ?? 0))
            return new AbilityLevelResult();

        var battleInfos = BuildHitMonsterInfos(targetEntities);

        var hasLiveMonster = targetEntities.OfType<EntityMonster>().Any(x => x.IsAlive);
        BattleInstance? instance = null;
        if (hasLiveMonster)
            instance = await Player.BattleManager!.StartBattle(attackEntity, targetEntities, req.SkillIndex == 1);

        return new AbilityLevelResult(instance, battleInfos);
    }

    private static List<HitMonsterInstance> BuildHitMonsterInfos(List<BaseGameEntity> targetEntities) =>
        targetEntities.Select(entity => new HitMonsterInstance(
            entity.EntityId,
            entity is EntityMonster { IsAlive: false }
                ? MonsterBattleType.DirectDieSkipBattle
                : MonsterBattleType.TriggerBattle)).ToList();

    // Resolve an avatar's technique (SkillMaze) maze buffs without depending on enum fields that
    // CyreneRes sometimes emits as raw integers (UseType/MazeBuffType/InBattleBindingType — e.g.
    // Constance 132101 has UseType=3, SilverWolf999 150601 has InBattleBindingType=6). Match purely
    // on the numeric avatar id (key/10/100) and the reliably-string InBattleBindingKey == "SkillMaze",
    // returning every matching buff (Firefly -> 131001 + 131002). Empty when the avatar has no
    // SkillMaze technique buff (e.g. Gilgamesh's chain field is not a SkillMaze maze buff): we never
    // inject a guessed id — that matches the reference server, which injects nothing for such avatars.
    public static List<(int BuffId, int Level, string UseType)> GetTechniqueMazeBuffs(int avatarId) =>
        GameData.MazeBuffData
            .Where(kvp => kvp.Key / 10 / 100 == avatarId && kvp.Value.InBattleBindingKey == "SkillMaze")
            .Select(kvp => (BuffId: kvp.Key / 10, Level: kvp.Value.Lv, kvp.Value.UseType))
            .Distinct()
            .ToList();

    // True when the avatar's technique is a battle-triggering ATTACK — its SkillMaze maze buff
    // carries UseType "TriggerBattle"(/IgnoreWeakness), e.g. Firefly's mech or Silver Wolf. Field /
    // summon techniques use UseType "SummonUnit" (Dahlia/Cyrene barriers) or "AddBattleBuff"
    // (Gilgamesh/TohsakaRin chains); their skillIndex-1 placement must NOT start a battle (engaging
    // shows "技能使用失败"), they engage via a separate skillIndex-0 normal attack. CyreneRes emits
    // some UseType as raw enum ints, but TriggerBattle reliably arrives as the string; non-trigger
    // ints/strings simply fail the prefix test. Data-driven, no avatar id hard-coded.
    public static bool HasBattleTriggerTechnique(int avatarId) =>
        GameData.MazeBuffData.Any(kvp =>
            kvp.Key / 10 / 100 == avatarId
            && kvp.Value.InBattleBindingKey == "SkillMaze"
            && kvp.Value.UseType.StartsWith("TriggerBattle", System.StringComparison.Ordinal));

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
