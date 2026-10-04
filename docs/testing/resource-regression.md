# 原始资源加载回归

项目：[`scripts/ResourceRegression/`](../../scripts/ResourceRegression/)。直接调用生产 `ResourceManager.LoadGameData()`，不启动服务、数据库或玩家对象，不读取生产 `Config.json`，不替换现有缓存。运营自定义配置与语言文件从仓库 `Config` 只读加载。

```powershell
dotnet run --project scripts/ResourceRegression/ResourceRegression.csproj -c Release -- <Resources目录> <新报告目录> --cache-out <新缓存文件>
dotnet run --project scripts/ResourceRegression/ResourceRegression.csproj -c Release -- <Resources目录> <新报告目录> --stale-cache <适配前缓存> --cache-out <新缓存文件>
```

`--cache-out` 可省略；指定时必须选择不存在的新文件。测试先做 MemoryPack 往返，再以 CreateNew 写入压缩快照。报告目录保存加载日志、audit.json 和隔离测试夹具。夹具故意触发缺文件与解析错误，最终生产资源加载阶段必须没有警告或错误，预期 `failures: 0, loader issues: 0`。

覆盖：可选表缺失允许加载、必需表缺失仍警告并禁止生成完整缓存、已存在的可选表解析错误仍失败、NPC 按角色声明的能力名解析共享配置、缺失或冲突的能力定义不能被当成空配置、新版 GridFight 枚举值、完整生产加载和快照往返。只读加载和缓存验证不能替代客户端实机任务或战斗验证。

OSBETAWin4.6.51 的四组 GridFight 枚举取自同 DLL 的显式枚举值。SpecificAvatarWithEquip 为 16，SpecificAvatarWithRandomEquip 为 17；旧资源缓存的数值含义可能不同，更新程序后应使用同版本资源生成新缓存，回滚时恢复配套程序和缓存。

该客户端索引不包含 DialogueDynamicContent、DialogueEvent、GridFightGenderOverride、ItemConfigTrainDynamic、RogueDLCBossBp、RogueWolfGunMiracleTarget、TrainPartyDynamicConfig。它们分别是没有当前读取方的历史/动态伴随表，或已有基础性别回退的覆盖表，按具体文件声明为可选；主 ItemConfig 等必需表不受影响。客户端实际包含的文件仍执行完整解析，不能用空 JSON 补齐不存在的表。

NPC 变体引用公共、原型怪物或 FakePlayer 配置，实体文件名不一定对应能力文件名。共享能力按角色 AbilityList 和 SkillList.EntryAbility 解析，冲突或真实缺失继续报错。未附带角色文件的 WhiteBox 开发记录不参与能力加载；这些测试记录不属于该客户端发布场景。

验证新缓存的 GridFight 回归可指定只读来源，程序会先复制到来源目录下的独立测试子目录：

```powershell
dotnet run --project scripts/GridFightRegression/GridFightRegression.csproj -- --cache <新缓存文件>
```

OSBETAWin4.6.51 新版表通过原始加载回归、缓存往返和 GridFight 路线回归。GridFightCampSeason 的 MonsterList 与基础阵营表合并，当前赛季通过阵营 ID 查询，其他赛季以内部复合键保留，发送的 ID 不改变。角色表没有重复 ID：第二赛季使用六位角色 ID，AvatarID 仍是基础角色 ID。新版不再要求 RoleSavedValueList，以原表的 GridFightRoleStar 解析技能；缓存记录该布局标志。第二赛季回血天赋使用 3011，阵型选择忽略 ID 为 0 的默认行。

`--stale-cache` 只在报告目录复制旧缓存，验证存在新版原表时能识别缺少适配结果的缓存，然后重新加载资源；不会替换生产缓存。旧版组合表保留原加载方式。按赛季筛选随机角色奖励，拒绝跨赛季角色加入。`IsFakeAvatar` 记录保留在缓存；帕姆 604202 没有普通 AvatarConfig，不加入普通角色池，NPC 专属战斗逻辑尚未实现。这些检查仍不能替代客户端实机战斗验证。

## 启动读取顺序与回退回归

启动优先加载 `Config.json` 中 `Path.ResourcePath` 指定的原始资源目录，即使已有缓存也先读取原表。目录不存在、指向文件、必需表缺失或不可读时直接读取 `Path.ConfigPath/Resource.bin`；解析异常或部分加载失败时，先清空已加载的数据，再恢复整份缓存。原始资源和缓存都不可用时停止启动。回退缓存不根据不可用的原表判定过期，但仍检查压缩、序列化格式和必需的角色、场景及装备数据。

`ServerOption.UseCache` 控制原始资源加载成功后是否写入缓存；设为 false 仍允许失败时读取已有缓存。只有成功加载原始资源才更新缓存，先写同目录临时文件，再替换 `Resource.bin`。缓存写入失败会保留已加载的原始数据，失败的原始加载不会覆盖旧缓存。缓存启动后继续加载运营自定义内容，手册只在原始资源启动时生成。

独立回归项目直接调用生产启动加载器，复制运营 JSON 和配套缓存到新报告目录，不启动数据库或网络，不修改生产资源与缓存：

```powershell
dotnet run --project scripts/ResourceStartupRegression/ResourceStartupRegression.csproj -c Release -- <资源目录> <配套缓存> <新报告目录>
```

覆盖原表优先、缺目录、路径指向文件、空/非法路径、空目录、缺必需表、文件独占锁、JSON 解析失败后恢复整份缓存、缺缓存/损坏缓存/缺关键数据时停止、UseCache 开关、缓存生成与替换失败。读取失败与故意破坏测试会产生预期警告/错误，以最终 `Resource startup regression ... failures: 0` 和退出码判断结果。

部署前备份程序与已有 `Config/Resource.bin`，确认资源路径后再切换到新程序。现在有效的原始目录优先于已有缓存；仅部署缓存时可使用不存在的原始资源路径。回滚时恢复配套程序、资源和缓存；如果只需恢复旧缓存读取行为，也必须同时恢复先前程序，避免有效原始目录再次生成缓存。
