using March7thHoney.Data;
using March7thHoney.Data.Config;
using March7thHoney.Database.Inventory;
using March7thHoney.Enums.Scene;
using March7thHoney.GameServer.Game.Drop;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.Scene;
using March7thHoney.GameServer.Game.Scene.Entity;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.Battle;

public class SceneSkillManager(PlayerInstance player) : BasePlayerManager(player)
{
    public async ValueTask<SkillResultData> OnCast(SceneCastSkillCsReq req)
    {
        await HandleDestructPropDrops(req);

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
        if (attackEntity == null) return new SkillResultData(Retcode.RetSceneEntityNotExist);
        // get ability file
        var abilities = GetAbilityConfig(attackEntity);
        if (abilities == null || abilities.AbilityList.Count < 1)
            return new SkillResultData(Retcode.RetMazeNoAbility);

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
        var res = await Player.TaskManager!.AbilityLevelTask.TriggerTasks(abilities, targetAbility.OnStart,
            attackEntity, targetEntities, req);

        // check if avatar execute
        if (attackEntity is AvatarSceneInfo) await Player.SceneInstance!.OnUseSkill(req);

        // backfill character maze buffs for techniques whose ability JSON spawns
        // a summon unit instead of calling AddMazeBuff (e.g. Dahlia 1321 -> 132101).
        // candidates = AvatarMazeBuff entries bound to the SkillMaze slot. If the
        // ability JSON already added one of them, skip entirely; otherwise apply
        // the canonical primary entry (UseType SummonUnit/AddBattleBuff/TriggerBattle)
        // so battleInfo.buffList picks it up. This keeps multi-entry avatars (Cyrene,
        // Cerydra, Robin...) from receiving stray summon/listener buffs.
        if (req.SkillIndex == 1 && attackEntity is AvatarSceneInfo casterAvatar)
        {
            var avatarId = casterAvatar.AvatarInfo.BaseAvatarId;
            var candidates = GameData.MazeBuffData
                .Where(kvp => kvp.Key / 10 / 100 == avatarId
                              && kvp.Value.MazeBuffType == "Character"
                              && kvp.Value.InBattleBindingType == "CharacterSkill"
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

    private async ValueTask HandleDestructPropDrops(SceneCastSkillCsReq req)
    {
        if (Player.SceneInstance == null || Player.InventoryManager == null) return;

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
            if (prop.State == PropStateEnum.Open) continue;

            await prop.SetState(PropStateEnum.Open);

            if (prop.PropInfo.EventID > 0)
            {
                var eventItems = BuildPlaneEventRewards(prop.PropInfo.EventID);
                if (eventItems.Count > 0) await Player.InventoryManager.AddItems(eventItems);
            }

            if (prop.PropInfo.ChestID <= 0) continue;
            var items = DropService.CalculateDropsFromProp(prop.PropInfo.ChestID);
            if (items.Count > 0) await Player.InventoryManager.AddItems(items);
        }
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
