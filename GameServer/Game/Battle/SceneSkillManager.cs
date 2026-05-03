using March7thHoney.Data;
using March7thHoney.Data.Config;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.Scene;
using March7thHoney.GameServer.Game.Scene.Entity;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.Battle;

public class SceneSkillManager(PlayerInstance player) : BasePlayerManager(player)
{
    public async ValueTask<SkillResultData> OnCast(SceneCastSkillCsReq req)
    {
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

        return new SkillResultData(Retcode.RetSucc, res.Instance, res.BattleInfos);
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
