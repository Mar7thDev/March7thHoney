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

覆盖范围：

- Shield、新版及旧版 Passport：三种 RSA 填充、1024/2048 位密钥、密钥不匹配、缺失/无效私钥、畸形密文、严格 UTF-8、明文兼容。解密失败必须在登录及自动注册前返回错误；同一账号多次随机加密只能得到一个账号。
- 楼层存档条件 AND/OR/NOT 真值表、各比较运算、缓存省略比较符时的等于语义。
- 恢复原有活动楼层置 `1` 规则：有关联活动任务的 Town/Maze 楼层，资源中默认为 `0` 的变量统一补为 `1`（`*_IsHidden` 除外），显式存档值仍然优先；不写入玩家存档。长乐天 `10202001/170` 虚构叙事和流梦礁 `10304001/145` 末日幻影入口，验证实际实体创建、传送下发、登录恢复、重复同步及存档覆盖，而非仅判断组开关。
- 传送成功和跨地图标志分别返回；无效入口、缺楼层/锚点/地图、递归加载中止不再报告成功。同地图传送、重载、剧情线保存位置传送保持可用。
- 协议数值标签：`SceneMapInfo.floor_id=14`、`retcode=6`；失败返回码独立编码，强制字段映射优先于旧版本推断，同时合并原始名与语义名作用域。
- 精确排除仙舟异常道具：保留星槎海中枢 `10201001/280/300001` 与长乐天 `10202001/208/300001` 两项 `PCOnly` 捕获排除，恢复旧安卓修复中的长乐天 `10202001/144/300003`、`10202001/208/300002` 排除。初始加载、直接创建、强制加载遵循同一规则；长乐天空组 `208`、星槎海中枢空组 `280` 不重复加载。
- 仅排除长乐天 `10202001/169` 异常氛围组，并在同步时移除残留组及实体；不依据 `IsHoyoGroup` 与 `Atmosphere` 组合全局排除。普通非初始道具、NPC、其他氛围组和储物舱段组 `8` 电梯继续可用。活动楼层全开规则保持不变，同时验证传送、登录恢复和重复同步不重新下发异常内容。兼容名单使用缓存中的楼层/组/实例，当前缓存无需重建。
- 登录恢复场景和传送场景都有 4.5 初始化值；两端内存 KCP 验证 `1491 → EnterSceneByServerScNotify → EnterSceneScRsp` 顺序，同地图移动、静默构建、无效传送和登录快照不会误发切换边界。
- Rogue 系列兼容：`MapEntranceLD.json` 已纳入入口资源；经典 Rogue 的 904 地图可由站点房间池完整生成；ChessRogue 在开始包省略骰面分支时选择完整默认分支；RogueMagic 查询包含非空区域及必需嵌套信息；差分宇宙大厅入口和徽标在无存档时默认开启。
- 经典模拟宇宙房间出口：锚点组的 `PROP_ROGUE_DOOR` 携带 `rogue_game_info{room_id, MAJKFIMPOOF}` 指向下一房间，战斗房间以 Closed 加载且忽略已存的道具状态；祝福待选时不开门，经真实 `HandleRogueCommonPendingActionCsReq` 选择后在响应前下发 Open 刷新；无祝福可选的战斗胜利直接开门；开门状态不写入楼层道具存档。
- 经典模拟宇宙离开：已胜利未离开的对局在重新登录时被丢弃而登录仍恢复其房间场景，此时 `LeaveRogueCsReq` 不结算、直接返回大厅（先 `1491` 再成功响应，附阵容与大厅场景，并清除额外阵容）；不在模拟宇宙房间时仍返回状态不匹配。
- 任务对话：开启任务系统的玩家在追踪主线唯一的 Talk 完成门（`1010202/101020206`）所在楼层发送 `FinishTalkMissionCsReq{talk_str:"0"}` 时只回 ACK，不完成任何任务；客户端上报自己的对话串后该子任务照常完成。
- 教程可关闭判定：触发条件含 `OnMazeMainPage` 的自由探索弹窗（如 `3006 → 1000511`）要求目标任务已完成，目标仅进行中时不可关闭；战斗内教程（如 `1001 → 1000101`）仍接受进行中或已完成；`TutorialFinish` 递归按目标教程自身的触发条件判定严格性。
- 主线开始条件：`AcceptMainMissionByCondition` 同时要求 `TakeParam` 与 `BeginParam`；`1010502` 在 `1010403` 完成后、开拓等级 11 时不接取，经验发放升到 13 级后自动接取；空或 `Auto` 的 `BeginParam` 不构成限制。

边界：这不是安卓客户端测试，不能据此宣称加载卡死已在实机消失。密钥不匹配会被明确拒绝；让对应客户端成功登录仍需配套的公私钥。本修复不删除或合并历史 Base64 账号。
