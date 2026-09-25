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
- 恢复原有活动楼层置 `1` 规则：有关联活动任务的 Town/Maze 楼层，资源中默认为 `0` 的变量统一补为 `1`（`*_IsHidden` 除外），显式存档值仍然优先；不写入玩家存档。长乐天 `10202001/170` 虚构叙事和流梦礁 `10304001/145` 末日幻影入口，验证实际实体创建、传送下发、登录恢复、重复同步及存档覆盖，而非仅判断组开关。
- 传送成功和跨地图标志分别返回；无效入口、缺楼层/锚点/地图、递归加载中止不再报告成功。同地图传送、重载、剧情线保存位置传送保持可用。
- 协议数值标签：`SceneMapInfo.floor_id=14`、`retcode=6`；失败返回码独立编码，强制字段映射优先于旧版本推断，同时合并原始名与语义名作用域。
- 精确排除仙舟异常道具：保留星槎海中枢 `10201001/280/300001` 与长乐天 `10202001/208/300001` 两项 `PCOnly` 捕获排除，恢复旧安卓修复中的长乐天 `10202001/144/300003`、`10202001/208/300002` 排除。初始加载、直接创建、强制加载遵循同一规则；长乐天空组 `208`、星槎海中枢空组 `280` 不重复加载。
- 仅排除长乐天 `10202001/169` 异常氛围组，并在同步时移除残留组及实体；不依据 `IsHoyoGroup` 与 `Atmosphere` 组合全局排除。普通非初始道具、NPC、其他氛围组和储物舱段组 `8` 电梯继续可用。活动楼层全开规则保持不变，同时验证传送、登录恢复和重复同步不重新下发异常内容。兼容名单使用缓存中的楼层/组/实例，当前缓存无需重建。
- 登录恢复场景和传送场景都有 4.5 初始化值；两端内存 KCP 验证 `1491 → EnterSceneByServerScNotify → EnterSceneScRsp` 顺序，同地图移动、静默构建、无效传送和登录快照不会误发切换边界。
- Rogue 系列兼容：`MapEntranceLD.json` 已纳入入口资源；经典 Rogue 的 904 地图可由站点房间池完整生成；ChessRogue 在开始包省略骰面分支时选择完整默认分支；RogueMagic 查询包含非空区域及必需嵌套信息；差分宇宙大厅入口和徽标在无存档时默认开启。
- 角色试用（TrialActivity）：`Config/Custom/ActivityConfig.json` 含全部 23 个试用模块排期；原始 `AvatarDemoConfig.json` 存在而缓存缺 `StageType` 时拒绝缓存，仅有缓存的部署不受影响。通过真实请求处理器与内存 KCP 验证：`SpecialDisplay` 和未知关卡返回 `RetReqParaInvalid` 且不影响正在进行的试用；开始 `315130` 使用 `2001381–2001384` 特殊角色、初始能量 `6500/6500/7500/7000`、关卡自身怪物事件，世界 `999` 解析为返回点世界；进入场景失败回滚到原试用；切换试用保留最初返回点；`315120` 战斗由怪物事件解析为 `315121`，无 StageConfig 的 `380060` 解析为 `380050`；战败不结算；胜利按 `TrialActivityDataChangeScNotify → 场景切换 → CurTrialActivityScNotify → PVEBattleResultScRsp` 顺序下发，只记录一次，回到返回点并移除试用队伍；离开回显关卡号且不记录通关；领奖回包首项为物品 `1`（星琼），重复领取、未通关、非试用关卡均被拒绝且不再发放；试用中重新登录会放弃试用、移除试用队伍并回到返回点。

边界：这不是安卓客户端测试，不能据此宣称加载卡死已在实机消失。密钥不匹配会被明确拒绝；让对应客户端成功登录仍需配套的公私钥。本修复不删除或合并历史 Base64 账号。
