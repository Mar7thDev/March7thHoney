# 原始资源加载回归

项目：[`scripts/ResourceRegression/`](../../scripts/ResourceRegression/)。直接调用生产 `ResourceManager.LoadGameData()`，不启动服务、数据库或玩家对象，不读取生产 `Config.json`，不替换现有缓存。运营自定义配置与语言文件从仓库 `Config` 只读加载。

```powershell
dotnet run --project scripts/ResourceRegression/ResourceRegression.csproj -c Release -- <Resources目录> <新报告目录> --cache-out <新缓存文件>
dotnet run --project scripts/ResourceRegression/ResourceRegression.csproj -c Release -- <Resources目录> <新报告目录> --stale-cache <适配前缓存> --cache-out <新缓存文件>
```

`--cache-out` 可省略；指定时必须选择不存在的新文件。测试先做 MemoryPack 往返，再以 CreateNew 写入压缩快照。报告目录保存加载日志、audit.json 和隔离测试夹具。`Logger` 把 ResMgr / ResCache 的告警只写入调试日志，回归因此从报告目录的 `loader-debug.log` 读回这些告警，并计入 `loader issues` 和 audit.json 的 `Warnings`。夹具故意触发缺文件与解析错误，最终生产资源加载阶段必须没有警告或错误，预期 `failures: 0, loader issues: 0`。

覆盖：可选表缺失允许加载、必需表缺失仍警告并禁止生成完整缓存、已存在的可选表解析错误仍失败、NPC 按角色声明的能力名解析共享配置、缺失或冲突的能力定义不能被当成空配置、新版 GridFight 枚举值、完整生产加载和快照往返。只读加载和缓存验证不能替代客户端实机任务或战斗验证。

OSBETAWin4.6.51 的四组 GridFight 枚举取自同 DLL 的显式枚举值。SpecificAvatarWithEquip 为 16，SpecificAvatarWithRandomEquip 为 17；旧资源缓存的数值含义可能不同，更新程序后应使用同版本资源生成新缓存，回滚时恢复配套程序和缓存。

当前代码与这段说明不一致：`GridFightBonusTypeEnum`、`GridFightTraitEffectTypeEnum`、`GridFightConsumeTypeEnum` 的高位成员和 `GridFightEquipFuncTypeEnum.AddTraitLayer` 用的是服务端占位值，现有 `Config/Resource.bin` 也按占位值生成，所以「beta bonus enum values match the client」这一项失败，`--cache-out` 不会写出缓存。OSBETAWin4.7.51 客户端的取值为：BonusType 的 SpecificAvatarWithEquip 16、SpecificAvatarWithRandomEquip 17、SpotLightReplaceCnt 18、Augment 19、SpotLightFreeRefreshCnt 20、TrailblazeShopCoin 21、ElationGoods 22、GlobalHp 23、ElationItem 24、AllAvatarByRarity 25；TraitEffectType 的 BandRoleChoose 19、MoonGame 24、MoonGameSpotLight 25、DanHengTwoInOne 28、JadeShop 29、ClaraEnhance 31、AdditionalTraitRoleChoose 32、AhaUnique 33、GainOtherRoleTrait 34；ConsumeType 的 UpgradeChimera 7、CopyChimera 8；EquipFuncType 的 AddTraitLayer 12。这些枚举只按名称读表、不上协议，改回客户端数值会改变缓存里整数的含义，必须和重新生成的缓存一起发布。

该客户端索引不包含 DialogueDynamicContent、DialogueEvent、GridFightGenderOverride、ItemConfigTrainDynamic、RogueDLCBossBp、RogueWolfGunMiracleTarget、TrainPartyDynamicConfig。它们分别是没有当前读取方的历史/动态伴随表，或已有基础性别回退的覆盖表，按具体文件声明为可选；主 ItemConfig 等必需表不受影响。客户端实际包含的文件仍执行完整解析，不能用空 JSON 补齐不存在的表。

NPC 变体引用公共、原型怪物或 FakePlayer 配置，实体文件名不一定对应能力文件名。共享能力按角色 AbilityList 和 SkillList.EntryAbility 解析，冲突或真实缺失继续报错。未附带角色文件的 WhiteBox 开发记录不参与能力加载；这些测试记录不属于该客户端发布场景。

验证新缓存的 GridFight 回归可指定只读来源，程序会先复制到来源目录下的独立测试子目录：

```powershell
dotnet run --project scripts/GridFightRegression/GridFightRegression.csproj -- --cache <新缓存文件>
```

OSBETAWin4.6.51 新版表通过原始加载回归、缓存往返和 GridFight 路线回归。GridFightCampSeason 的 MonsterList 与基础阵营表合并，当前赛季通过阵营 ID 查询，其他赛季以内部复合键保留，发送的 ID 不改变。角色表没有重复 ID：第二赛季使用六位角色 ID，AvatarID 仍是基础角色 ID。新版不再要求 RoleSavedValueList，以原表的 GridFightRoleStar 解析技能；缓存记录该布局标志。第二赛季回血天赋使用 3011，阵型选择忽略 ID 为 0 的默认行。

`--stale-cache` 只在报告目录复制旧缓存，验证存在新版原表时能识别缺少适配结果的缓存，然后重新加载资源；不会替换生产缓存。旧版组合表保留原加载方式。按赛季筛选随机角色奖励，拒绝跨赛季角色加入。`IsFakeAvatar` 记录保留在缓存；帕姆 604202 没有普通 AvatarConfig，不加入普通角色池，NPC 专属战斗逻辑尚未实现。这些检查仍不能替代客户端实机战斗验证。

部署前备份程序与已有 `Config/Resource.bin`，确认资源路径后再切换到新程序和新缓存。加载器优先读取缓存；仅修改 ResourcePath 不会替换已有缓存。只有客户端、没有完整原始资源时，按[从客户端导出重建资源缓存](../operations/resource-cache-from-client-export.md)处理。
