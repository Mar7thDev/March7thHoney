using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using March7thHoney.Data;
using March7thHoney.Data.Config;
using March7thHoney.Data.Config.Task;
using March7thHoney.Enums.Avatar;
using March7thHoney.Enums.RogueMagic;
using March7thHoney.Enums.Scene;
using March7thHoney.GameServer.Game.Battle;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.Scene;
using March7thHoney.GameServer.Game.Scene.Component;
using March7thHoney.GameServer.Game.Scene.Entity;
using March7thHoney.GameServer.Server.Packet.Send.Lineup;
using March7thHoney.GameServer.Server.Packet.Send.Scene;
using March7thHoney.Proto;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Game.Task.AvatarTask;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
public class AbilityLevelTask(PlayerInstance player)
{
    public PlayerInstance Player { get; set; } = player;

    // buff ids this cast's ability delivers to enemies via attack-detect (SummonUnitTriggerAttackDetectConfig); the maze-buff backfill skips these so they aren't also applied to the caster
    public HashSet<int> SummonAttackDetectBuffs { get; } = [];

    #region Selector

    public async ValueTask<object> TargetAlias(AbilityLevelParam param)
    {
        await ValueTask.CompletedTask;
        var selector = param.TargetEvaluator!;
        var casterEntity = param.CasterEntity;
        var targetEntities = param.TargetEntities;

        if (selector is TargetAlias target)
            return target.Alias switch
            {
                "Caster" or "ModifierOwnerEntity" => [casterEntity],
                "ParamEntity" or "AllEnemy" or "AbilityTargetEntity" => targetEntities,
                "AdvTeamMembers" => Player.SceneInstance!.AvatarInfo.Values.ToList().OfType<BaseGameEntity>().ToList(),
                "AdvLocalPlayer" => Player.SceneInstance!.AvatarInfo.Values
                    .Where(x => x.AvatarInfo.BaseAvatarId == Player.LineupManager!.GetCurLineup()?.LeaderAvatarId)
                    .OfType<BaseGameEntity>().ToList(),
                _ => targetEntities
            };

        return new List<BaseGameEntity>();
    }

    #endregion

    #region Manage

    public async ValueTask<AbilityLevelResult> TriggerTasks(AdventureAbilityConfigListInfo abilities,
        List<TaskConfigInfo> tasks, BaseGameEntity casterEntity, List<BaseGameEntity> targetEntities,
        SceneCastSkillCsReq req,
        string? modifierName = null)
    {
        BattleInstance? instance = null;
        List<HitMonsterInstance> battleInfos = [];
        foreach (var task in tasks)
            try
            {
                var res = await TriggerTask(new AbilityLevelParam(abilities, task, casterEntity, targetEntities, req,
                    modifierName));
                if (res.BattleInfos != null) battleInfos.AddRange(res.BattleInfos);

                if (res.Instance != null) instance = res.Instance;
            }
            catch (Exception e)
            {
                Logger.GetByClassName().Error("An error occured, ", e);
            }

        return new AbilityLevelResult(instance, battleInfos);
    }

    public async ValueTask<AbilityLevelResult> TriggerTask(AbilityLevelParam param)
    {
        try
        {
            var methodName = param.Act.Type.Replace("RPG.GameCore.", "");

            // try to get from cache
            var method = GetOrCreateExecuteTask(methodName);
            if (method == null) return new AbilityLevelResult();

            var res = method(param);
            var re = await res;
            if (re is AbilityLevelResult result) return result;
        }
        catch (Exception e)
        {
            Logger.GetByClassName().Error("An error occured, ", e);
        }

        return new AbilityLevelResult();
    }

    private ExecuteTask? GetOrCreateExecuteTask(string methodName)
    {
        // try to get from cache
        if (_cachedTasks.TryGetValue(methodName, out var method)) return method;
        var methodProp = GetType().GetMethod(methodName);
        if (methodProp == null) return null;

        method = (ExecuteTask)Delegate.CreateDelegate(typeof(ExecuteTask), this, methodProp);
        _cachedTasks[methodName] = method; // cached

        return method;
    }

    private delegate ValueTask<object> ExecuteTask(AbilityLevelParam param);

    private readonly ConcurrentDictionary<string, ExecuteTask> _cachedTasks = [];

    #endregion

    #region Task

    public async ValueTask<object> PredicateTaskList(AbilityLevelParam param)
    {
        BattleInstance? instance = null;
        List<HitMonsterInstance> battleInfos = [];

        if (param.Act is PredicateTaskList predicateTaskList)
        {
            // handle predicateCondition
            var methodName = predicateTaskList.Predicate.Type.Replace("RPG.GameCore.", "");

            var method = GetOrCreateExecuteTask(methodName);
            var res = true;
            if (method != null)
            {
                var resp = await method(param with { Act = predicateTaskList.Predicate });
                if (resp is not bool r)
                    res = false;
                else
                    res = predicateTaskList.Predicate.Inverse ? !r : r;
            }

            if (res)
                foreach (var task in predicateTaskList.SuccessTaskList)
                {
                    var result = await TriggerTask(param with { Act = task });
                    if (result.BattleInfos != null) battleInfos.AddRange(result.BattleInfos);

                    if (result.Instance != null) instance = result.Instance;
                }
            else
                foreach (var task in predicateTaskList.FailedTaskList)
                {
                    var result = await TriggerTask(param with { Act = task });
                    if (result.BattleInfos != null) battleInfos.AddRange(result.BattleInfos);

                    if (result.Instance != null) instance = result.Instance;
                }
        }

        return new AbilityLevelResult(instance, battleInfos);
    }

    // 官服秘技随机奖励(如欢愉「燃起来了!」): 客户端在 cast 请求里回传 roll 结果 _result, 服务端据此选择加哪支 maze buff
    public ValueTask<object> AdvByCompareDynamicValue(AbilityLevelParam param)
    {
        if (param.Act is not Data.Config.Task.AdvByCompareDynamicValue cmp)
            return ValueTask.FromResult<object>(true);

        var dv = param.Request?.DynamicValues.FirstOrDefault(x => x.Key == cmp.DynamicKey);
        // 请求未带该键或对比值是动态表达式(本服务端不求值)时, 保持原默认放行行为
        if (dv == null || cmp.CompareValue.IsDynamic)
            return ValueTask.FromResult<object>(!cmp.Inverse);

        var left = dv.Value;
        var right = cmp.CompareValue.GetValue();
        var res = cmp.CompareType switch
        {
            "Equal" => Math.Abs(left - right) < 0.0001f,
            "NotEqual" => Math.Abs(left - right) >= 0.0001f,
            "Greater" => left > right,
            "GreaterEqual" => left >= right,
            "Less" => left < right,
            "LessEqual" => left <= right,
            _ => true
        };
        return ValueTask.FromResult<object>(res);
    }

    public async ValueTask<object> AdventureTriggerAttack(AbilityLevelParam param)
    {
        BattleInstance? instance = null;
        List<HitMonsterInstance> battleInfos = [];

        if (param.Act is AdventureTriggerAttack adventureTriggerAttack)
        {
            // 解析命中目标：优先用 AttackTargetType 求值，缺失时回落到客户端在请求里报告的命中怪物
            List<BaseGameEntity> target;
            if (!string.IsNullOrEmpty(adventureTriggerAttack.AttackTargetType.Type))
            {
                var methodName = adventureTriggerAttack.AttackTargetType.Type.Replace("RPG.GameCore.", "");
                var method = GetOrCreateExecuteTask(methodName);
                if (method == null) return new AbilityLevelResult();
                var resp = await method(param with { TargetEvaluator = adventureTriggerAttack.AttackTargetType });
                if (resp is not List<BaseGameEntity> t) return new AbilityLevelResult();
                target = t;
            }
            else
            {
                target = [.. param.TargetEntities];
            }

            foreach (var task in adventureTriggerAttack.OnAttack)
            {
                var result = await TriggerTask(param with { Act = task, TargetEntities = target });
                if (result.BattleInfos != null) battleInfos.AddRange(result.BattleInfos);
            }

            // attack-detect 场域：把召唤物对应 trigger 的 maze buff 下发给被攻击的敌人（如吉尔伽美什 150901，随敌人带进战斗）
            var detect = adventureTriggerAttack.SummonUnitTriggerAttackDetectConfig;
            if (detect != null && Player.SceneInstance != null)
            {
                var summon = Player.SceneInstance.SummonUnit.Values
                    .FirstOrDefault(x => x.SummonUnitId == (int)detect.SummonUnitID);
                if (summon != null)
                {
                    // 记下经 attack-detect 投放的 buff，供 backfill 排除（避免同一 buff 又被加到施法者，如 150901 致冻）
                    var trig = summon.TriggerList.FirstOrDefault(t => t.TriggerName == detect.DetectSummonUnitTriggerName);
                    if (trig != null)
                        foreach (var t in trig.OnTriggerEnter)
                            if (t is AddMazeBuff amb)
                                SummonAttackDetectBuffs.Add(amb.ID);

                    var detectIds = target.OfType<EntityMonster>().Where(x => x.IsAlive)
                        .Select(x => (uint)x.EntityId).ToList();
                    if (detectIds.Count > 0)
                        await Player.SceneInstance.TriggerSummonUnit(summon.EntityId,
                            detect.DetectSummonUnitTriggerName, detectIds);
                }
            }

            // 开战判定：
            // - TriggerBattle: true（旧式 ability 显式声明）
            // - EnterBattleSelectTargetType 非空（新式，如 Evanescia 的 AllHitTarget）
            // - 普攻（SkillIndex == 0）命中怪物：开放世界进入战斗的核心机制，
            //   官方依赖客户端在请求里报告 assistMonsterEntityIdList 而非 ability 配置驱动
            var hasHitMonster = target.OfType<EntityMonster>().Any(x => x.IsAlive);
            var shouldTriggerBattle = adventureTriggerAttack.TriggerBattle
                                      || !string.IsNullOrEmpty(adventureTriggerAttack.EnterBattleSelectTargetType)
                                      || (param.Request.SkillIndex == 0 && hasHitMonster);

            if (target.Count > 0 && shouldTriggerBattle)
            {
                foreach (var task in adventureTriggerAttack.OnBattle)
                {
                    var result = await TriggerTask(param with { Act = task, TargetEntities = target });
                    if (result.BattleInfos != null) battleInfos.AddRange(result.BattleInfos);
                }

                foreach (var entity in target)
                {
                    var type = MonsterBattleType.TriggerBattle;
                    if (entity is EntityMonster { IsAlive: false })
                        type = MonsterBattleType.DirectDieSkipBattle;

                    battleInfos.Add(new HitMonsterInstance(entity.EntityId, type));
                }

                instance = await Player.BattleManager!.StartBattle(param.CasterEntity, target,
                    param.Request.SkillIndex == 1);
            }
        }

        return new AbilityLevelResult(instance, battleInfos);
    }

    // 停云秘技（技能 120207）：立即为施法者恢复 50 点能量。唯一使用 AdventureModifyTeamPlayerSP 的角色。
    // 引擎 SP 为「能量 ×100」，满能量归一化为 10000，故 50 点 = 5000。
    private const int TechniqueSpRecoverFallback = 5000;

    public async ValueTask<object> AdventureModifyTeamPlayerSP(AbilityLevelParam param)
    {
        if (param.Act is not Data.Config.Task.AdventureModifyTeamPlayerSP modifySp)
            return new AbilityLevelResult();

        // 解析作用目标（停云秘技为 Caster 自身）；缺失求值器时回落到施法者
        List<BaseGameEntity> targets;
        var methodName = modifySp.SpecifyTargetType.Type.Replace("RPG.GameCore.", "");
        var method = GetOrCreateExecuteTask(methodName);
        if (method != null &&
            await method(param with { TargetEvaluator = modifySp.SpecifyTargetType }) is List<BaseGameEntity> resolved &&
            resolved.Count > 0)
            targets = resolved;
        else
            targets = [param.CasterEntity];

        // 客户端下发的是动态表达式（PostfixExpr），本服务端无表达式求值；非动态时直接使用，
        // 否则回落到停云秘技固定恢复值。
        var recover = modifySp.ModifyValue.GetValue() * 100;
        if (recover <= 0) recover = TechniqueSpRecoverFallback;

        var lineup = Player.LineupManager!.GetCurLineup();
        var isExtra = lineup?.IsExtraLineup() ?? false;

        var changed = false;
        foreach (var avatar in targets.OfType<AvatarSceneInfo>())
        {
            var info = avatar.AvatarInfo;
            info.SetCurSp(Math.Min(info.GetCurSp(isExtra) + recover, 10000), isExtra);
            changed = true;
        }

        // 同步队伍能量，使覆盖世界 UI 立即生效；下一次进战由 ToBattleProto 读取已更新的 SP 带入战斗
        if (changed && lineup != null)
            await Player.SendPacket(new PacketSyncLineupNotify(lineup));

        return new AbilityLevelResult();
    }

    public async ValueTask<object> AddMazeBuff(AbilityLevelParam param)
    {
        BattleInstance? instance = null;
        List<HitMonsterInstance> battleInfos = [];

        if (param.Act is AddMazeBuff addMazeBuff)
        {
            var methodName = addMazeBuff.TargetType.Type.Replace("RPG.GameCore.", "");
            var method = GetOrCreateExecuteTask(methodName);
            if (method == null) return new AbilityLevelResult();
            var resp = await method(param with { TargetEvaluator = addMazeBuff.TargetType });

            Dictionary<string, float> dynamic = [];
            foreach (var dynamicValue in addMazeBuff.DynamicValues)
                dynamic.Add(dynamicValue.Key, dynamicValue.Value.GetValue());

            if (resp is not List<BaseGameEntity> target) return new AbilityLevelResult(instance, battleInfos);

            var time = addMazeBuff.LifeTime.FixedValue.Value < -1 ? 20 :
                addMazeBuff.LifeTime.FixedValue.Value is > 30 or < 10 ? -1 : addMazeBuff.LifeTime.FixedValue.Value;

            foreach (var entity in target)
            {
                var buffOwnerAvatarId = (param.CasterEntity as AvatarSceneInfo)?.AvatarInfo.BaseAvatarId ?? 0;
                var summonCaster = param.CasterEntity as EntitySummonUnit;
                if (summonCaster != null) buffOwnerAvatarId = summonCaster.CreateAvatarId;

                await entity.AddBuff(new SceneBuff(addMazeBuff.ID, 1, buffOwnerAvatarId, time)
                {
                    DynamicValues = dynamic,
                    // Buff source is cast request entity when available, otherwise current caster entity.
                    CastEntityId = param.Request.CastEntityId == 0
                        ? param.CasterEntity.EntityId
                        : (int)param.Request.CastEntityId,
                    // For summon-unit driven buffs, official uses attach entity id as summon reference.
                    SummonUnitEntityId = summonCaster?.AttachEntityId ?? 0
                });
            }
        }

        return new AbilityLevelResult(instance, battleInfos);
    }

    public async ValueTask<object> RemoveMazeBuff(AbilityLevelParam param)
    {
        BattleInstance? instance = null;
        List<HitMonsterInstance> battleInfos = [];

        if (param.Act is RemoveMazeBuff removeMazeBuff)
        {
            var methodName = removeMazeBuff.TargetType.Type.Replace("RPG.GameCore.", "");
            var method = GetOrCreateExecuteTask(methodName);
            if (method == null) return new AbilityLevelResult();
            var resp = await method(param with { TargetEvaluator = removeMazeBuff.TargetType });

            if (resp is not List<BaseGameEntity> target) return new AbilityLevelResult(instance, battleInfos);

            foreach (var entity in target.OfType<AvatarSceneInfo>())
                await entity.RemoveBuff(removeMazeBuff.ID);
        }

        return new AbilityLevelResult(instance, battleInfos);
    }

    public async ValueTask<object> AdventureFireProjectile(AbilityLevelParam param)
    {
        BattleInstance? instance = null;
        List<HitMonsterInstance> battleInfos = [];

        if (param.Act is AdventureFireProjectile adventureFireProjectile)
        {
            foreach (var task in adventureFireProjectile.OnProjectileHit)
            {
                var result = await TriggerTask(param with { Act = task });
                if (result.BattleInfos != null) battleInfos.AddRange(result.BattleInfos);

                if (result.Instance != null)
                    instance = result.Instance;
            }

            foreach (var task in adventureFireProjectile.OnProjectileLifetimeFinish)
            {
                var result = await TriggerTask(param with { Act = task });
                if (result.BattleInfos != null) battleInfos.AddRange(result.BattleInfos);

                if (result.Instance != null)
                    instance = result.Instance;
            }
        }

        return new AbilityLevelResult(instance, battleInfos);
    }

    public async ValueTask<object> NewAdventureFireProjectile(AbilityLevelParam param)
    {
        BattleInstance? instance = null;
        List<HitMonsterInstance> battleInfos = [];

        if (param.Act is AdventureFireProjectile adventureFireProjectile)
        {
            foreach (var task in adventureFireProjectile.OnProjectileHit)
            {
                var result = await TriggerTask(param with { Act = task });
                if (result.BattleInfos != null) battleInfos.AddRange(result.BattleInfos);

                if (result.Instance != null)
                    instance = result.Instance;
            }

            foreach (var task in adventureFireProjectile.OnProjectileLifetimeFinish)
            {
                var result = await TriggerTask(param with { Act = task });
                if (result.BattleInfos != null) battleInfos.AddRange(result.BattleInfos);

                if (result.Instance != null)
                    instance = result.Instance;
            }
        }

        return new AbilityLevelResult(instance, battleInfos);
    }

    public async ValueTask<object> CreateSummonUnit(AbilityLevelParam param)
    {
        if (param.Act is CreateSummonUnit createSummonUnit)
        {
            if (!GameData.SummonUnitDataData.TryGetValue(createSummonUnit.SummonUnitID, out var excel))
                return new AbilityLevelResult();

            var fallbackMotion = new MotionInfo
            {
                Pos = Player.Data.Pos?.ToProto() ?? new Vector(),
                Rot = Player.Data.Rot?.ToProto() ?? new Vector()
            };
            var spawnMotion = param.Request.TargetMotion ?? fallbackMotion;
            if (spawnMotion.Pos == null) spawnMotion.Pos = fallbackMotion.Pos;
            if (spawnMotion.Rot == null) spawnMotion.Rot = fallbackMotion.Rot;

            const int summonLifeTimeMs = 45 * 1000;

            // 领域型召唤绑施法者(客户端才做领域检测上报 RefreshTrigger); 排除攻击侦测型(走服务端攻击侦测, 绑了会被客户端 OnTriggerExit 误删 buff, 如吉尔 150901)
            var attachPoint = excel.ConfigInfo?.AttachPoint;
            var attachToCaster = attachPoint == "Origin"
                                 || (string.IsNullOrEmpty(attachPoint) && !IsAttackDetectSummon(param.AdventureAbility, excel.ID));

            var unit = new EntitySummonUnit
            {
                EntityId = 0,
                CreateAvatarEntityId = param.CasterEntity.EntityId,
                AttachEntityId = attachToCaster ? param.CasterEntity.EntityId : 0,
                SummonUnitId = excel.ID,
                CreateAvatarId = (param.CasterEntity as AvatarSceneInfo)?.AvatarInfo.BaseAvatarId ?? 0,
                LifeTimeMs = summonLifeTimeMs,
                TriggerList = excel.ConfigInfo?.TriggerConfig.CustomTriggers ?? [],
                Motion = spawnMotion
            };

            await Player.SceneInstance!.AddSummonUnitEntity(unit);
        }

        return new AbilityLevelResult();
    }

    // 召唤体是否被某角色技能用作攻击侦测(SummonUnitTriggerAttackDetectConfig); 此类走服务端攻击侦测, 不绑施法者以免客户端领域触发的 OnTriggerExit 误删其 buff
    private static bool IsAttackDetectSummon(AdventureAbilityConfigListInfo abilities, int summonUnitId)
    {
        return abilities.AbilityList.Any(a =>
            ContainsAttackDetect(a.OnStart, summonUnitId) || ContainsAttackDetect(a.OnAdd, summonUnitId)
            || ContainsAttackDetect(a.OnRemove, summonUnitId) || ContainsAttackDetect(a.OnAbort, summonUnitId));
    }

    private static bool ContainsAttackDetect(List<TaskConfigInfo> tasks, int summonUnitId)
    {
        foreach (var t in tasks)
        {
            if (t is AdventureTriggerAttack { SummonUnitTriggerAttackDetectConfig: { } d } && d.SummonUnitID == (uint)summonUnitId)
                return true;
            if (t is PredicateTaskList p
                && (ContainsAttackDetect(p.SuccessTaskList, summonUnitId) || ContainsAttackDetect(p.FailedTaskList, summonUnitId)))
                return true;
        }

        return false;
    }

    public async ValueTask<object> DestroySummonUnit(AbilityLevelParam param)
    {
        if (param.Act is DestroySummonUnit destroySummonUnit)
            await Player.SceneInstance!.RemoveSummonUnitById(destroySummonUnit.SummonUnit.SummonUnitID); // TODO

        return new AbilityLevelResult();
    }

    public async ValueTask<object> AddAdventureModifier(AbilityLevelParam param)
    {
        if (param.Act is AddAdventureModifier addAdventureModifier)
        {
            GameData.AdventureModifierData.TryGetValue(addAdventureModifier.ModifierName, out var modifier);
            if (modifier == null) return new AbilityLevelResult();

            if (param.CasterEntity is IGameModifier mod) await mod.AddModifier(addAdventureModifier.ModifierName);
        }

        return new AbilityLevelResult();
    }

    public async ValueTask<object> RemoveAdventureModifier(AbilityLevelParam param)
    {
        if (param.Act is RemoveAdventureModifier removeAdventureModifier)
        {
            GameData.AdventureModifierData.TryGetValue(removeAdventureModifier.ModifierName, out var modifier);
            if (modifier == null) return new AbilityLevelResult();

            if (param.CasterEntity is IGameModifier mod) await mod.RemoveModifier(removeAdventureModifier.ModifierName);
        }

        return new AbilityLevelResult();
    }

    public async ValueTask<object> RemoveSelfModifier(AbilityLevelParam param)
    {
        if (param.ModifierName != null)
            if (param.CasterEntity is IGameModifier mod)
                await mod.RemoveModifier(param.ModifierName);

        return new AbilityLevelResult();
    }

    public async ValueTask<object> RefreshMazeBuffTime(AbilityLevelParam param)
    {
        if (param.Act is RefreshMazeBuffTime refreshMazeBuffTime)
        {
            // get buff
            var buff = param.CasterEntity.BuffList.FirstOrDefault(x => x.BuffId == refreshMazeBuffTime.ID);
            if (buff == null) return new AbilityLevelResult();
            buff.Duration = refreshMazeBuffTime.LifeTime.GetValue();
            await Player.SendPacket(new PacketSyncEntityBuffChangeListScNotify(param.CasterEntity, buff));
        }

        return new AbilityLevelResult();
    }

    public async ValueTask<object> AdvModifyMaxMazeMP(AbilityLevelParam param)
    {
        await ValueTask.CompletedTask;

        if (param.Act is AdvModifyMaxMazeMP advModifyMaxMazeMp)
            switch (advModifyMaxMazeMp.ModifyFunction)
            {
                case PropertyModifyFunctionEnum.Add:
                    Player.LineupManager!.Data.ExtraMpCount += advModifyMaxMazeMp.ModifyValue.GetValue();
                    break;
                case PropertyModifyFunctionEnum.Set:
                    Player.LineupManager!.Data.ExtraMpCount = advModifyMaxMazeMp.ModifyValue.GetValue() - 5;
                    break;
            }

        return new AbilityLevelResult();
    }

    public async ValueTask<object> AdventureSetAttackTargetMonsterDie(AbilityLevelParam param)
    {
        var avatar = param.CasterEntity as AvatarSceneInfo;
        if (GameData.AvatarConfigData.TryGetValue(avatar?.AvatarInfo.AvatarId ?? 0, out var excel))
        {
            var adventurePlayerExcel = GameData.AdventurePlayerData.GetValueOrDefault(excel.AdventurePlayerID);
            if (adventurePlayerExcel != null && adventurePlayerExcel.MazeSkillIdList.Count > param.Request.SkillIndex)
            {
                var skill = GameData.MazeSkillData.GetValueOrDefault(
                    adventurePlayerExcel.MazeSkillIdList[(int)param.Request.SkillIndex]);

                await Player.LineupManager!.CostMp(skill?.MPCost ?? 1, param.Request.CastEntityId);
            }
        }

        foreach (var targetEntity in param.TargetEntities)
        {
            if (targetEntity is not EntityMonster monster) continue;

            if (monster.MonsterData.Rank < MonsterRankEnum.Elite)
            {
                await monster.Kill();

                // 4.2 proto migration in progress: rogue extension rewards are temporarily disabled here.
            }
        }

        return new AbilityLevelResult();
    }

    #endregion

    #region Predicate

    public async ValueTask<object> ByAllowInstantKill(AbilityLevelParam param)
    {
        await ValueTask.CompletedTask;

        foreach (var targetEntity in param.TargetEntities)
            if (targetEntity is EntityMonster monster)
                if (monster.MonsterData.Rank < MonsterRankEnum.Elite)
                    return true;

        return false;
    }

    public async ValueTask<object> ByIsContainAdventureModifier(AbilityLevelParam param)
    {
        await ValueTask.CompletedTask;

        if (param.Act is ByIsContainAdventureModifier byIsContain)
        {
            // get target
            var result = false;
            var methodName = byIsContain.TargetType.Type.Replace("RPG.GameCore.", "");

            var method = GetOrCreateExecuteTask(methodName);
            if (method == null) return false;

            var resp = await method(param with { TargetEvaluator = byIsContain.TargetType });

            if (resp is List<BaseGameEntity> target)
                foreach (var entity in target)
                {
                    if (entity is not IGameModifier modifier) continue;
                    if (modifier.Modifiers.Contains(byIsContain.ModifierName))
                    {
                        result = true;
                        break;
                    }
                }

            return result;
        }

        return false;
    }

    public async ValueTask<object> AdventureByInMotionState(AbilityLevelParam param)
    {
        await ValueTask.CompletedTask;

        return true;
    }

    public async ValueTask<object> AdventureByPlayerCurrentSkillType(AbilityLevelParam param)
    {
        await ValueTask.CompletedTask;

        if (param.Act is AdventureByPlayerCurrentSkillType byPlayerCurrentSkillType)
            return param.Request.SkillIndex == (uint)byPlayerCurrentSkillType.SkillType;

        return false;
    }

    public async ValueTask<object> ByCompareCarryMazebuff(AbilityLevelParam param)
    {
        await ValueTask.CompletedTask;

        if (param.Act is ByCompareCarryMazebuff byCompareCarryMazebuff)
            return param.CasterEntity.BuffList.Any(x => x.BuffId == byCompareCarryMazebuff.BuffID);

        return false;
    }

    public async ValueTask<object> ByAnd(AbilityLevelParam param)
    {
        await ValueTask.CompletedTask;

        if (param.Act is ByAnd byAnd)
        {
            foreach (var task in byAnd.PredicateList)
            {
                var methodName = task.Type.Replace("RPG.GameCore.", "");

                var method = GetOrCreateExecuteTask(methodName);
                if (method == null) return false;

                var resp = await method(param with { Act = task });
                if (resp is not bool res) return false;
                res = task.Inverse ? !res : res;
                if (!res) return false;
            }

            return true;
        }

        return false;
    }

    #endregion
}

public record AbilityLevelResult(BattleInstance? Instance = null, List<HitMonsterInstance>? BattleInfos = null);

public record AbilityLevelParam(
    AdventureAbilityConfigListInfo AdventureAbility,
    TaskConfigInfo Act,
    BaseGameEntity CasterEntity,
    List<BaseGameEntity> TargetEntities,
    SceneCastSkillCsReq Request,
    string? ModifierName,
    TargetEvaluator? TargetEvaluator = null);
