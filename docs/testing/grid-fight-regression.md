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
- 赛季 2 每局随机一名伙伴（`GridFightPetPal.json`，跳过无效果的 1 号）及其一条效果，经 `GridFightGameInfo` 的伙伴同步（字段 1984，条目字段 9 为伙伴、8 为效果）和结算 `GridFightFinishInfo`（字段 1836）下发；客户端开局流程直接读取第一条伙伴效果，缺失时空引用导致“开始对局”无反应。伙伴效果本身的玩法尚未实现。
- 对局结算（胜利或退出结算）触发 `GridFightFinishGamePlay` 任务进度，参数 1 为标准博弈、2 为超频博弈；超频博弈由 `GridFight_OC_UnlockQuest`（7302115）解锁。
- 地图控制台（`Prop_Common_GridFightConsole_01`）按 `ByCompareMainMissionState(8037100)` 分支，省略的状态取类默认值 `Finish`：`GridFight_Tutorial_Main_Mission_Id` 已完成时打开新入口，否则打开赛季 1 旧页面。服务端如实下发该任务状态，覆盖见 `LoginSceneRegression`。
- 标准路线按赛季取：`GridFightStageRoute` 无赛季列，赛季 N 的段位路线编号从 N×1000 起（赛季 1 为 1000–1800、1801，23 节点；赛季 2 为 2000–2800、2801，16 节点），1000 以下为教程/首玩路线。
- 4.6.51 客户端只用 `GridFightLevelInfo` 字段 12 下发的路线节点构建关卡节点（章节 5、小节 1，参数内 2 关卡、7 惩罚规则、8 增益节点、12 基础金币、14 节点类型）；缺失时当前节点为空，进度条显示 0-0、备战页无法出战。
- 赛季 2 路线中补给节点之后可能紧接精英分支，补给回合开始后进入精英遭遇选择（未经抓包确认，依据为客户端需要遭遇选择才能出战）。
- 消耗品增删同步必须带嵌套的 `GridFightConsumableInfo`（`GridFightConsumableUpdateInfo` 字段 13），客户端只读该字段。
- 赛季 2 等级表新增只能由效果达到的 11 级；普通经验（购买、战斗）只升到 `GridFight_DefaultPlayerLevelMax`（10），10 级无升级经验，不能再滚入 11 级。

- 补给卡片（`GridFightSupplyRoleInfo`）必须带字段 13 的角色掉落（`GridFightDropEntry` 角色/1 星），装备放在字段 6 的掉落列表；客户端 `GridFightDraftItemData.Create` 无条件读取字段 13，缺失时空引用，表现为卡在补给节点（如 1-3）的结算。

### 赛季 2 羁绊效果

协议结构来自 4.6.51 客户端反汇编（`artifacts/gf-season2/re-g1..g5-*.md`，未纳入版本控制），不是抓包；客户端不包含的规则（商品池、触发节点、阈值）按羁绊描述实现，代码中注明为推测。
新表（模板装备、白厄薪火、克拉拉改装、奇美拉、开拓商店、翡翠黑市、欢愉模板）尚未进入 `Resource.bin`，由 `artifacts/gf-season2/gen_s2_tables.py` 生成为 `GridFightSeasonTwoTables.cs`。

- 客户端自行发起的交互走 `GridFightTraitUpdateCsReq`（8657），客户端在收到 `GridFightTraitUpdateScRsp` 前锁定输入，失败也必须回包；服务端提示走待办动作。
- 每个币战请求处理后由 `GridFightHandler<T>` 刷新赛季 2 状态，并只把发生变化的效果快照（`GridFightTraitSyncInfo`）下发；只对已激活羁绊下发，客户端对未知羁绊的同步会空引用。
- 5 费独立羁绊：双龙吟（饮月上场召唤同星级腾荒于后台，不占人口、不可出售/前移；选择 召唤/治疗/连击/反击 之一赋予饮月）、盗火行者（拖拽角色给白厄转化为薪火，存于角色存档值 `GP_Avatar_S2_Phainon_00`；黄金裔名单为推测）、巡海游侠（不死途在场时任命一名前台角色，3 星时全体前台）、黑市主理人（1 号槽招牌商品不售罄，轮换商品按翡翠星级生成）、机心巧匠（上场立即改装，前 3 次每节点、之后每 2 节点一次）、幻乐之主（阿哈 3/4/5 费档不升星，多余拷贝转为成长值并升费，阈值 3/6 为推测，9 级与欢愉/幻月游戏总星级升 7 费）。
- 其他羁绊：开拓银轨（每场战斗积攒开拓力、开拓商店、列车升级作为战斗 MazeBuff）、千星商会（1 档刷新得经验、2 档每节点免费刷新）、奇兽花园（每节点 1 点能量，首次达到各档免费招聘）、幻月游戏（每局随机规则，愿力随战斗累积）、星核猎手/黄金大饭店（每局按权重选模板，随层数发放模板装备）、梦境交织、反击王、仙舟舰队（战斗内基础装备升级）、欢愉（阿哈装备栏，拖入装备与简易装备合成）。
- 未实现：克拉拉改装在战斗中的效果（改装 BuffID 不在本地 MazeBuff 表，未知 MazeBuff 会导致关卡无法开始）、阿哈消耗品、欢愉重铸/换模板待办、千星商会 3–6 档、幻月游戏唱名规则、聚光灯与 2101–2214 传送门效果。

回归中的赛季 2 羁绊检查（仅赛季 2 缓存运行）覆盖：腾荒召唤/人口/出售与移动限制/随饮月离场、双龙吟选择与羁绊归属、白厄吞噬与薪火存档值、巡海游侠任命、翡翠黑市槽位与招牌商品、开拓商店商品与战后开拓力、克拉拉首次改装、奇美拉免费招聘、全部 5 费独立羁绊同时在场时 GetData 的协议往返与 ContextValue。

回归中的赛季隔离检查覆盖：角色池、传送门与投资池、商店 300 次刷新、补给、全部可解析宝珠各 8 次开启、锻造、随机装备、合成、图鉴、骰子填充与完整路线中出现的角色/装备/报价。

缓存组成：`Config/Resource.bin` 由 4.6.51 资源生成，再用 4.6.0 正式服缓存补齐 4.6.51 dump 缺失的表和行（任务、消息、MechCraft 等）；GridFight 表一律以 4.6.51 为准，不从旧缓存补行（旧缓存中的 14 条赛季 1 词缀不再混入）。

验证结果（2026-10-04，加入赛季 2 羁绊后）：赛季 2 缓存 177/177（赛季 2 路线更短，战斗相关检查随之减少），4.6.0 赛季 1 缓存 194/194。

下载版资源缓存不能由当前数据模型反序列化（`VideoKeysConfig` 字段布局不匹配），因此保留当前缓存，并使用当前缓存完成验证。
这些检查验证服务端状态及消息构造；客户端界面、动画和实机战斗尚未验证。下载版注释中的抓包结论未在本次重新抓包核实。
