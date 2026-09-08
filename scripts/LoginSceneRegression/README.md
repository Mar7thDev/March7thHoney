# 4.5 PROD 登录与场景回归

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
- 长乐天 `10202001` 空存档不再强制开启活动变量，组 `170` 随真实状态加载；服务器实体加载、动态同步和下发数据使用同一维度默认值，显式保存的 `0` 优先。
- 传送成功和跨地图标志分别返回；无效入口、缺楼层/锚点/地图、递归加载中止不再报告成功。同地图传送、重载、剧情线保存位置传送保持可用。
- 协议数值标签：`SceneMapInfo.floor_id=14`、`retcode=6`；失败返回码独立编码，强制字段映射优先于旧版本推断，同时合并原始名与语义名作用域。
- 长乐天 `144/300003`、`208/300001`、`208/300002` 三个已验证异常的初始道具继续被精确排除；其他道具与 NPC 不因不可靠的 `LoadOnInitial=false` 永久丢失，组级任务/存档条件仍然生效，空组 `208` 不重复加载。
- 氛围组仅在 `IsHoyoGroup=true` 时排除；长乐天 `169` 不生成，非 Hoyo 氛围组以及储物舱段组 `8` 的服务端电梯仍可加载。升级后的缓存无需原始 JSON 即可使用。
- 登录恢复场景和传送场景都有 4.5 初始化值；两端内存 KCP 验证 `1491 → EnterSceneByServerScNotify → EnterSceneScRsp` 顺序，同地图移动、静默构建、无效传送和登录快照不会误发切换边界。

边界：这不是安卓客户端测试，不能据此宣称加载卡死已在实机消失。密钥不匹配会被明确拒绝；让对应客户端成功登录仍需配套的公私钥。本修复不删除或合并历史 Base64 账号。
