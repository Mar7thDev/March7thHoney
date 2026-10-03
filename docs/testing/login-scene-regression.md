# 4.5 PROD 登录与场景回归

测试项目：[`scripts/LoginSceneRegression/`](../../scripts/LoginSceneRegression/)

从仓库根目录运行：

```powershell
dotnet run --project scripts/LoginSceneRegression/LoginSceneRegression.csproj -c Release
```

测试使用随机 RSA 密钥、临时 SQLite 数据库与仅监听本机的 HTTP 服务；不会读取运营配置、连接生产服务器或改写现有数据库。场景测试读取当前 `Config/Resource.bin` 的副本，不重新生成缓存。临时产物路径在测试结束时输出。

如有升级前缓存，可额外验证旧缓存反序列化和过期检查：

```powershell
dotnet run --project scripts/LoginSceneRegression/LoginSceneRegression.csproj -c Release -- --legacy-cache artifacts/Resource.before-scene-fix-DB152E0F-20260907.bin
```

角色试用用例需要带 `AvatarDemoConfig.StageType`/`SPList` 的缓存。尚未替换 `Config/Resource.bin` 时，可用原始资源重新生成的缓存验证（只复制到临时目录，不改写该文件）：

```powershell
dotnet run --project scripts/LoginSceneRegression/LoginSceneRegression.csproj -c Release -- --resource-cache <新生成的 Resource.bin>
```

缓存缺少这两个字段时，测试在角色试用部分明确报错停止。

覆盖范围：

- Shield、新版及旧版 Passport：三种 RSA 填充、1024/2048 位密钥、密钥不匹配、缺失/无效私钥、畸形密文、严格 UTF-8、明文兼容。解密失败必须在登录及自动注册前返回错误；同一账号多次随机加密只能得到一个账号。
- 楼层存档条件 AND/OR/NOT 真值表、各比较运算、缓存省略比较符时的等于语义。
- 恢复原有活动楼层置 `1` 规则：有关联活动任务的 Town/Maze 楼层，资源中默认为 `0` 的变量统一补为 `1`（`*_IsHidden` 除外），显式存档值仍然优先；不写入玩家存档。启用任务系统时 `SceneStateByMainMission`/`WorldStateByMainMission` 不再补 `1`，保留资源默认值（官方 `20001001` 下发 `0`），由剧情写入的存档值决定；未启用任务时仍补 `1`。长乐天 `10202001/170` 虚构叙事和流梦礁 `10304001/145` 末日幻影入口，验证实际实体创建、传送下发、登录恢复、重复同步及存档覆盖，而非仅判断组开关。
- 传送成功和跨地图标志分别返回；无效入口、缺楼层/锚点/地图、递归加载中止不再报告成功。同地图传送、重载、剧情线保存位置传送保持可用。
- 协议数值标签：`SceneMapInfo.floor_id=14`、`retcode=6`；失败返回码独立编码，强制字段映射优先于旧版本推断，同时合并原始名与语义名作用域。
- 精确排除仙舟异常道具：保留星槎海中枢 `10201001/280/300001` 与长乐天 `10202001/208/300001` 两项 `PCOnly` 捕获排除，恢复旧安卓修复中的长乐天 `10202001/144/300003`、`10202001/208/300002` 排除。初始加载、直接创建、强制加载遵循同一规则；长乐天空组 `208`、星槎海中枢空组 `280` 不重复加载。
- 仅排除长乐天 `10202001/169` 异常氛围组，并在同步时移除残留组及实体；不依据 `IsHoyoGroup` 与 `Atmosphere` 组合全局排除。普通非初始道具、NPC、其他氛围组和储物舱段组 `8` 电梯继续可用。活动楼层全开规则保持不变，同时验证传送、登录恢复和重复同步不重新下发异常内容。兼容名单使用缓存中的楼层/组/实例，当前缓存无需重建。
- 召唤体领域 buff（`20001001` 真实怪物，昔涟 `141501` → 召唤体 `14151` → 敌人 `141502`）：召唤体投放的 buff 以召唤体自身实体 ID 作为 `BuffSummonEntityId`，保留资源时长 `500`，每个敌人独立 buff 实例，离场刷新为 `1`；由有限时长 maze buff 的 modifier 创建的召唤体下发 `LifeTimeMs=-1`，随该 buff 移除而销毁并带走投放的 buff；owning buff 永久时仍用 45 秒兜底；`RemoveMazeBuffOnDestroy=false` 的召唤体（`14101`）销毁后保留已投放 buff。
- 登录恢复场景和传送场景都有 4.5 初始化值；两端内存 KCP 验证 `1491 → EnterSceneByServerScNotify → EnterSceneScRsp` 顺序，同地图移动、静默构建、无效传送和登录快照不会误发切换边界。
- Rogue 系列兼容：`MapEntranceLD.json` 已纳入入口资源；经典 Rogue 的 904 地图可由站点房间池完整生成；ChessRogue 在开始包省略骰面分支时选择完整默认分支；RogueMagic 查询包含非空区域及必需嵌套信息；差分宇宙大厅入口和徽标在无存档时默认开启。
- 试用角色虚拟队伍：移除最后一名试用角色时拆除虚拟队伍并恢复自身队伍，按 `ExtraLineupDestroyNotify → SyncLineupNotify` 顺序通知客户端。
- `/spawn`：参数解析（`lv`/`hp`/`s`/`x`/`r`、`/`、`|`、`\` 分波及每波 5 只自动分波、无效 ID 汇总）与 ID 解析（`monster`、NPC 怪物、战斗 MonsterID 经模板映射、物件、纯战斗怪物）；长乐天无怪物配置时报告无模板；在资源中首个含可用怪物组的 Maze 楼层验证怪物借用已加载组的组/实例/EventID、偏移半径与零旋转，战斗中自定义波次替换关卡波次（波次号 1..N、固定关卡、等级与血量覆盖），仅血量覆盖时保留关卡波次；混入真实怪物的战斗仍计入任务；生成物件为 Open 状态、不带模板的事件/宝箱/交互且状态不写入存档；无效 ID、不存在的关卡、实体上限、缺少 ID 及模拟宇宙场景均不生成实体。
- 角色试用（TrialActivity）：`Config/Custom/ActivityConfig.json` 含 4.6 官方排期中全部 19 个试用模块（面板 20011/20064/20087/20091/20092/20093）；`Config/Custom/Banners.json` 完整加载 4.6 的 15 个卡池，且 UP 与展示物品都存在于资源缓存；原始 `AvatarDemoConfig.json` 存在而缓存缺 `StageType` 时拒绝缓存，仅有缓存的部署不受影响。通过真实请求处理器与内存 KCP 验证：`SpecialDisplay` 和未知关卡返回 `RetReqParaInvalid` 且不影响正在进行的试用；开始 `315130` 使用 `2001381–2001384` 特殊角色、初始能量 `6500/6500/7500/7000`、关卡自身怪物事件，世界 `999` 解析为返回点世界；进入场景失败回滚到原试用；切换试用保留最初返回点；`315120` 战斗由怪物事件解析为 `315121`，无 StageConfig 的 `380060` 解析为 `380050`；战败不结算；胜利按 `TrialActivityDataChangeScNotify → 场景切换 → CurTrialActivityScNotify → PVEBattleResultScRsp` 顺序下发，只记录一次，回到返回点并移除试用队伍；离开回显关卡号且不记录通关；领奖回包首项为物品 `1`（星琼），重复领取、未通关、非试用关卡均被拒绝且不再发放；试用中重新登录会放弃试用、移除试用队伍并回到返回点。
- 全服聊天：默认联系人 uid `98` 与指令机器人并列显示、在线且置顶，好友申请被拒绝；按 uid 精确查询资料。消息以 `[昵称 / uid]: 内容` 从该联系人转发给所有接收者（含发送者），不进入指令机器人会话；空消息忽略，超长与限流只回复发送者且不转发；滑动窗口按发送者独立计数；每人历史裁剪到 `MaxHistory`，历史末尾附欢迎语；uid 与指令机器人冲突或关闭功能时联系人消失且不转发。Discord Webhook 未在回归中发出真实请求。
- `/giveall avatar`：不带参数时已拥有角色的等级、星魂和当前命途保持不变，新角色为 1 级 / 0 魂；显式 `r` 写入角色已拥有的全部命途且不改等级，显式 `l` 不改星魂；开拓者只解锁当前性别的命途。
- `/language`：控制台不能固定语言；别名不区分大小写，只接受 `Config/Languages` 中存在文件的语言；固定后以新语言回复，`PlayerCommandSender` 使用固定语言，`auto` 清除后按客户端语言回复；固定值写入 `Player` 表，旧表自动补充可空 `LanguagePin` 列并读为未固定。
- 开拓者命途：资源中开拓者命途按奇数男 / 偶数女配对；存档保留两种性别的命途，但 `GetAvatarDataScRsp` 的命途列表与 `BasicTypeIdList`、`PlayerSyncScNotify` 只下发当前性别；切换命途时按性别归一并返回实际存储的 id，其他角色的命途不能套到开拓者上，`/hero type` 拒绝非开拓者命途；切换性别后仍保留原存档命途。
- 经典模拟宇宙房间出口：锚点组的 `PROP_ROGUE_DOOR` 携带 `rogue_game_info{room_id, MAJKFIMPOOF}` 指向下一房间，战斗房间以 Closed 加载且忽略已存的道具状态；祝福待选时不开门，经真实 `HandleRogueCommonPendingActionCsReq` 选择后在响应前下发 Open 刷新；无祝福可选的战斗胜利直接开门；开门状态不写入楼层道具存档。
- 经典模拟宇宙祝福：缓存保留经典 `RogueBuffExcel` 与差分宇宙 `RogueTournBuffExcel` 子类型，`RogueBuffGroup` 按 4.5 混淆键加载，`100005` 等祝福组完整解析为经典祝福；原始 `RogueBuffGroup.json` 存在时，过期检查接受当前缓存、拒绝祝福组塌缩到 id `0` 的旧缓存。首个战斗房间胜利后从真实祝福组抽出 3 个经典祝福，两次 `SyncRogueCommonPendingActionScNotify` 下发同一选项，并可选入对局。
- 经典模拟宇宙离开：已胜利未离开的对局在重新登录时被丢弃而登录仍恢复其房间场景，此时 `LeaveRogueCsReq` 不结算、直接返回大厅（先 `1491` 再成功响应，附阵容与大厅场景，并清除额外阵容）；不在模拟宇宙房间时仍返回状态不匹配。
- 任务对话：开启任务系统的玩家在追踪主线唯一的 Talk 完成门（`1010202/101020206`）所在楼层发送 `FinishTalkMissionCsReq{talk_str:"0"}` 时只回 ACK，不完成任何任务；客户端上报自己的对话串后该子任务照常完成。
- 教程可关闭判定：触发条件含 `OnMazeMainPage` 的自由探索弹窗（如 `3006 → 1000511`）要求目标任务已完成，目标仅进行中时不可关闭；战斗内教程（如 `1001 → 1000101`）仍接受进行中或已完成；`TutorialFinish` 递归按目标教程自身的触发条件判定严格性。
- 主线开始条件：`AcceptMainMissionByCondition` 同时要求 `TakeParam` 与 `BeginParam`；`1010502` 在 `1010403` 完成后、开拓等级 11 时不接取，经验发放升到 13 级后自动接取；空或 `Auto` 的 `BeginParam` 不构成限制。
- 经典模拟宇宙命令号：祝福强化监听 `1858` 并在 `1849` 回复（`5571/5588` 属于 ChessRogue）；`PickRogueAvatarCsReq`（`1846`）在 `1840` 回显两组角色：请求 #14 特殊角色写入响应 #9，请求 #9 基础角色写入响应 #2。
- 差分宇宙（4.6 Persona 流程，代码在 `scripts/LoginSceneRegression/RogueTournRegression.cs`）：缓存含 `RoguePersonaLayerRoom`/`RoomPreset`/`RoomCompType`/`StyleGift`、`RogueTournExpReward` 与祝福池 `TournMode`，Tourn3 祝福池按 MazeBuffID 去重；事件 `722001` 的 Act 图解析出 9 个选项且每个选项可结束对话。永久天赋只解锁树根，周挑战 id 取服务器配置。区域 `20405` 开局：三层房间数 4/5/4、房间 unique id 全局 1..N，层平面取自配置的 `PlanePool`，每层独立难度（3051/3052/3053）且首层怪物等级 86/87/88；预设格重放预设卡（首房 Battle lv3、各层末 Boss lv1、第三层 Respite）；两个无来源的开局 modifier 与风格选择。首房以 TournRogue 场景加载 3 只 86 级房间怪（`DPCNDADDCAJ=1`），房门关闭；快照含 5 个黑板标志、开局 modifier、牌组常量，未到达房间的组合字段存在但为空；房间注册一次列出全部房间。风格确认生成带档位的风格 modifier 对并打开方程式；方程式选入对局与图鉴；清空房间后房间完成、开门并发放与风格一致的 3 选 1 礼物；选礼物后打开 3 张卡的选房，选卡后进入下一格（卡片身份换入该格、三张卡全部消耗），层内换房复用同一场景实例且位置版本 +1；层信息推送为单房间增量。`blessall` 发放 Tourn3 全部祝福、风格档位到上限并写入场景 buff。最后一房清空后结束对局，结算按配置计分（score×10 经验），归档写入、经验发放且不可重复结算；不存在的经验等级不发奖；旧版无 unique id 的对局数据加载时自动编号；`/du reset` 清除对局。不经过真实战斗，房间以场景内清怪的方式结算。
- 模拟宇宙大厅图鉴机械臂（`80112001/3/300003`、`300008`，`Prop_Rogue_Gallery`）：无存档时，任务系统关闭或模拟宇宙教程主线 `4030002` 已完成则以 Open 加载，教程主线未完成时保持 Closed。
- 临时场景（Raid）运行：存档中进行中的 Raid 已不在当前版本 `RaidConfig` 中时，重新登录放弃该运行、删除记录并回到进入点（`OldEntryId`/`OldPos`），离开该 Raid 同样回到进入点并删除记录；运行记录丢失时离开回到默认出生点 `2000101`；`RaidConfig` 中存在的 Raid 默认仍在登录时恢复。重置用的 `RaidInfoNotify` 保留空 `item_list`。
- `ServerOption.ResetLocationOnLogin`（默认 `false`，后台设置页可改）：开启后每次登录放弃进行中的 Raid（删除记录）与挑战/星启运行，并把位置重置到默认出生点 `2000101` 再加载场景；剧情线进行中时不生效。
- `CommonRogueQueryScRsp`：查询类型即 `CCKJAEDDDCL` oneof 分支的字段号，按当前协议生成代码解析（4.6 的功能解锁分支 `BBEKBJPHEJK` 为 `1`，4.5 为 `15`），解锁集合只挂在该分支上，其余三类保持无分支。
- `SocialPlayGetDataCsReq`、`PetMarbleGetDataCsReq`、`PetMarbleGetCycleTrialPetsCsReq` 有处理器，回包中所有单值子消息（递归 5 层，oneof 与列表除外）均以空消息下发，而不是未处理请求的空包体。
- 4.6 盲盒宠物：缓存中盲盒宠物物品（251501 起）与一代召唤宠物（PetConfig 物品）可区分；新号 `GetPetDataScRsp` 带空的 `cur_pet` 与编队；获得盲盒宠物时每一份推送一条 `PetDataChangeScNotify{add_pet}`，一代宠物不成为实例；宠物物品不出现在 `PlayerSyncScNotify` 与 `GetBagScRsp` 的材料列表但仍保存在背包；实例 id 稳定、跨重新登录与数据库往返不变；设置编队先推送通知再回包，未拥有 / 重复实例、重复槽位、超过 3 槽均被拒绝且不改存档；移除宠物后实例与编队引用同步删除；召唤回显 `cur_pet`，收回返回空 `cur_pet` 并被记住；`BlindBoxGetData` 返回池 `1001`。
- 遗器预设：已拥有角色 + 已拥有遗器才能新建，未知遗器、未拥有角色、不带任何改动的更新被拒绝；改名生效。
- 队伍槽位：缺编号、额外队伍先插入的旧存档按编号补齐 0..8，下发的 `index` 与槽位编号一致；当前队伍指向空槽时改用编号最小的非空队伍；`MarkPresetLineupCsReq` 以 cmd `773`（`SetTeamFavourite`）回包并保存常用标记，非普通队伍编号返回 `RetLineupNotExist`；加标记前保存的 `LineupInfo` 仍可反序列化。
- 开拓者当前命途：女性存档上的男性当前命途改为对应女性命途，非命途 id 回落到已拥有的同性别命途，且 `GetAvatarDataScRsp` 的命途列表包含该命途；校正幂等。
- 视频密钥：`VideoKeyInfo` 的 `video_key` 写入字段 6（`HHCAKADKHAN`）、`ps_field_3` 写入字段 13（`BOGECOHIINH`），以 id `5600` 的官服值核对。
- 挑战结算（代码在 `scripts/LoginSceneRegression/ChallengeRegression.cs`）：虚构叙事取缓存中 ID 最大的两关挑战，直接喂战斗结果而不打真实战斗。场景内仍有怪时胜利记分并留在第 1 关；退出不改写分数（旧逻辑会把负差值转成约 42 亿）；重打时客户端回报的累计分只减去另一关分数，第 1 关保留先前得分；第 2 关只减第 1 关得分；最终结算把完成状态与第 2 关分数写入挑战存档，历史记录拆分总分与两关得分。异相仲裁小怪关在客户端未回传 type-5 目标列表时按未完成处理而不抛异常，回传时照常计星。

边界：这不是安卓客户端测试，不能据此宣称加载卡死已在实机消失。密钥不匹配会被明确拒绝；让对应客户端成功登录仍需配套的公私钥。本修复不删除或合并历史 Base64 账号。
