# GridFight 对比与回归验证

测试项目：[`scripts/GridFightRegression/`](../../scripts/GridFightRegression/)

本次对比基于当前 `4.5` 分支与 `C:\Users\Administrator\Downloads\GridFight`。
排除了仅换行不同的文件，迁入 GridFight 会话、资源目录、战斗消息和请求处理器的功能改动。

迁入内容包括传送门与契约、投资奖励、银河学者、命运卜者、银狼升级选择、装备与羁绊追踪、商店锁定及相关同步消息。
保留当前分支的非 GridFight 战斗逻辑、资源加载器与配置模型，没有覆盖任务、场景、抽卡等无关改动。

在下载版基础上的修复：

- 自动穿戴统一检查装备锁定、穿戴条件与槽位，显式目标无效时不转移到其他角色。
- 装备追踪批量更新先验证再提交，完成状态由实际穿戴推导，避免客户端伪造或残留状态影响自动穿戴。
- NPC 装备创建时即锁定，避免先被追踪系统穿给玩家角色。
- 统一手动与自动宝钻合成配方，保留满星角色及切换形态的商店过滤，并处理候选池为空的情况。
- 保留骰子绑定与战后重随，补齐临时装备删除同步，禁止单独转移临时装备，出售与合成角色时回收临时装备。
- 保留装备按备战阶段产金；重复备战请求不重复发奖。
- 串行发布银狼、命运卜者的选择，避免同一队列位置重复占用；选择期间禁止移动、出售目标角色或进入战斗。
- 接通周期性投资的节点回调，并在黄金期货触发后重置间隔计数。
- 使用当前 `CmdIds` 常量，移除空的迁移占位文件和未使用的清理方法。

运行：

```powershell
dotnet run --project scripts/GridFightRegression/GridFightRegression.csproj
dotnet run --project scripts/GridFightRegression/GridFightRegression.csproj -- --cache <Resource.bin>
dotnet build March7thHoney.sln --no-restore
```

回归程序默认读取当前 `Config/Resource.bin`；`--cache` 先把指定缓存复制到 `tmp/GridFightRegression/Cache/` 再读取。不初始化玩家数据库，不写回资源缓存；测试状态和额外测试装备仅存在于进程内。
覆盖全部可选传送门开局与 protobuf 往返、装备追踪、宝钻、骰子、选择队列、20 场战斗的完整路线、周期奖励、重复请求及中止结算。
赛季由缓存中的 `GridFight_Season_ID` 决定：测试角色取当前赛季角色池；银狼/命运卜者选择队列和持续伤害契约只在赛季 1 运行。

## 赛季 2 与赛季隔离

4.6.51（4.7 测试服）资源把 `GridFight_Season_ID` 改为 2，并且赛季 1、赛季 2 的行同表共存：
`GridFightRoleBasicInfo`（赛季 2 角色编号为头像 ID × 100 + 2，另有成本档变体）、`GridFightTraitBasicInfo`、
`GridFightEquipment` / `Consumables` / `Forge`、`GridFightCraftConfig`（同一对基础装备在两季合成出不同物品，如 350201+350201 → 35030101 / 47030101）。
`GridFightSeasonTalent` 则把同一批 ID（2011–4061）整体改为赛季 2 的天赋。

服务端处理：

- 新加载 `GridFightSeasonItem.json`、`GridFightSeasonCraft.json`（物品/合成 → 赛季），由 `GridFightSeasonScope` 统一判定；没有这两张表的旧缓存视为全部共享。
- 商店、开局、补给、宝珠/奖励（随机角色、随机装备、指定角色）、锻造、骰子、合成、图鉴只取当前赛季内容；`SpecificAvatar` 与 `101000+ID` 角色宝珠按头像 ID 解析为当前赛季角色。
- 4.6.51 去掉了 `GridFightCamp.SeasonID` 与 `RoleBasicInfo.RoleSavedValueList`：无赛季的阵营视为共享；缺少 SavedValue 列时角色池改用赛季 2 规则（排除 `IsFakeAvatar` 与阿哈 151112/151122/151132 成本档，七费档按 `ShopPrice` 第 7 行定价）。
- 赛季 1 专属机制只在赛季 1 生效：昔涟/银狼 LV999 校验、受限角色种子、4.5 商店稀有度表、赛季天赋 2011 终局回合治疗及 3560xxxx 天赋战斗 buff、天赋 4032 的宝珠奖励（`GridFightRewardRules.json` 的 `SeasonIds`）。
- `GridFightFormationWave` 新增一条省略 ID（0）的 5 人波次，战斗仍使用编号 5 的波次。
- 赛季 2 每局随机一名伙伴（`GridFightPetPal.json`，跳过无效果的 1 号）及其一条效果，经 `GridFightGameInfo` 的伙伴同步（字段 1984）和结算 `GridFightFinishInfo`（字段 1836）下发；客户端开局流程直接读取第一条伙伴效果，缺失时空引用导致“开始对局”无反应。伙伴效果本身的玩法尚未实现。
- 对局结算（胜利或退出结算）触发 `GridFightFinishGamePlay` 任务进度，参数 1 为标准博弈、2 为超频博弈；超频博弈由 `GridFight_OC_UnlockQuest`（7302115）解锁。
- 赛季 2 起向客户端隐藏已完成的 `GridFight_Tutorial_Main_Mission_Id`（8037100）：地图控制台只在该任务“未接取”时打开赛季 2 入口，已完成时打开赛季 1 旧页面；服务端自身仍视其为已完成。覆盖见 `LoginSceneRegression`。

赛季 2 专属玩法（聚光灯、奇美拉、开拓商店、欢愉笑点、阿哈升级等）及其 4.7 协议消息尚未实现：选择 2101–2214 传送门只记录为已选，不触发专属效果。

回归中的赛季隔离检查覆盖：角色池、传送门与投资池、商店 300 次刷新、补给、全部可解析宝珠各 8 次开启、锻造、随机装备、合成、图鉴、骰子填充与完整路线中出现的角色/装备/报价。

缓存组成：`Config/Resource.bin` 由 4.6.51 资源生成，再用 4.6.0 正式服缓存补齐 4.6.51 dump 缺失的表和行（任务、消息、MechCraft 等）；GridFight 表一律以 4.6.51 为准，不从旧缓存补行（旧缓存中的 14 条赛季 1 词缀不再混入）。

验证结果（2026-10-03）：赛季 2 缓存 168/168，4.6.0 赛季 1 缓存 188/188。

下载版资源缓存不能由当前数据模型反序列化（`VideoKeysConfig` 字段布局不匹配），因此保留当前缓存，并使用当前缓存完成验证。
这些检查验证服务端状态及消息构造；客户端界面、动画和实机战斗尚未验证。下载版注释中的抓包结论未在本次重新抓包核实。

## 4.7 测试服表兼容

检测到新版原生技能布局时，回归进入 `BetaTableRegression`，根据当前表中的角色、阵营和赛季进行检查，不复用旧版角色 ID 断言。覆盖全部传送门开局与消息往返、分表合并、怪物缺失与重复赛季的原子拒绝、旧表保留、跨赛季奖励隔离，以及从开局到最终首领的完整战斗和结算路线。旧版缓存仍执行原有装备、宝钻、投资及选择队列测试。

新版基础表和 GridFightCampSeason 合并后，当前赛季的阵营保留原 ID 索引，其他赛季以 `(SeasonID << 20) | CampID` 的内部键保存，行内 ID 和协议 ID 不变。新版角色通过 GridFightRoleStar 解析技能，缓存保存布局标志；旧缓存缺少这一适配结果且原表可用时自动要求重建。更新程序和缓存前保留备份，回滚时一起恢复。假角色（如第二赛季帕姆王 604202）不进入商店卡池，但仍可由奖励发放；第二赛季不沿用被复用的天赋 2011 做终局回合治疗。
