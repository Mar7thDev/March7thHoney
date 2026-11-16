# Proto

[English](protobuf.md) | 简体中文

本文说明服务器的协议定义如何保存、构建以及搬运到新版本的客户端。给协议类型或字段改名、重新生成命令号或升级到新客户端之前请先阅读。

Source: [`Proto/`](../../Proto/)

服务器使用的全部协议定义放在一个文件 `Proto/StarRail.proto` 里。`Proto.csproj` 在构建时把它生成为 `obj/<configuration>/<framework>/protobuf/StarRail.cs` 再编译，生成的 C# 不纳入版本控制。`main` 只保存当前游戏版本的定义，旧版本的定义留在各自的版本分支上。

## 目录

```text
Proto/
  StarRail.proto        当前版本的全部消息和枚举，构建直接编译这个文件
  overrides-manual.txt  搬运名称时无法按身份跟随的逐条人工审定，附证据
  cmdid-aliases.txt     命令号别名：服务端代码使用的命令名 = 现有常量名
  Build/                MSBuild 自动生成目标
  Tool/ProtoOrganizer/  向新版本搬运名称、生成命令号常量的工具
  obj/.../protobuf/     构建时生成的 C#，Git 忽略
```

Demiurge 的原始 dump 和搬运报告是中间产物，不进仓库。4.8 及更早的版本分支仍是按版本分目录、按 `Cmd*Type` 分组的旧布局，对应的工具和说明也在那些分支里。

## StarRail.proto 的格式

- 只有顶层的 `message` 和 `enum`，按名称排序，没有 `import` 和嵌套类型。
- 第一行的注释记录它来自哪个客户端 dump。
- 类型前的 `// Obf:` 记录它的混淆名，`// CmdID:` 记录承载它的命令号。这两行是向下一个版本搬运名称的依据，手工改名时保留。
- 尚未确认含义的类型和字段保持 11 位大写的混淆名。
- `Cmd<模块>Type` 枚举是各模块的命令号目录，成员以承载该命令号的消息命名。它们由工具生成，没有代码引用。

给混淆名补语义名时直接改这个文件；类型改名要同时替换所有引用它的字段类型。不要增删字段、改字段号或改字段类型：这些必须与客户端一致，而且下一次搬运会因此拒绝执行（见下文）。改完后正常构建即可，不要手改 `obj/`。

## 构建与验证

```sh
dotnet restore March7thHoney.sln
dotnet build March7thHoney.sln -c Release --no-restore
python3 scripts/test-proto-generation.py
```

NuGet 固定 `Google.Protobuf.Tools 3.33.1`（protoc 33.1），与现有 `Google.Protobuf 3.33.1` 运行时一致；两个版本都在仓库根目录的 `Directory.Packages.props` 中声明。首次构建需要还原此工具包；工具不会进入服务器发布产物。Linux、Windows 使用宿主平台工具，交叉发布不改变宿主选择。该版本 macOS 工具为 x64，Apple Silicon 需要 Rosetta；也可用 `-p:ProtocolCompiler=<path>` 指向版本严格匹配的 protoc 33.1。

构建目标检测 schema 的新增、删除、修改和缺失的生成文件；无变化时跳过生成。`dotnet clean` 后再次构建会恢复生成代码。

## 命令号

`KcpSharp/CmdIds.cs` 由工具从 `StarRail.proto` 生成：每个命令号一个常量，以承载它的消息命名，后面是 `cmdid-aliases.txt` 里的别名。改了带 `// CmdID:` 的消息名或增删别名之后重新生成：

```sh
dotnet run --project Proto/Tool/ProtoOrganizer -c Release -- cmdids
```

别名指向常量名而不是数值，命令号变化时不需要改别名文件。`cmdids --check` 只比较、不写入，`scripts/verify-security.py` 用它确认常量与 schema 一致。

命令枚举名不一定等于 protobuf 消息类型名，处理器绑定当前命令常量并解析真实消息类型；仅按枚举名搜索同名 `message` 会漏掉现有结构。请求没有处理器时框架自动发送空成功响应，不能用该响应判断功能已执行。

## 升级到新版本的客户端

名称能跨版本沿用，依据是混淆身份在版本间稳定：类型的混淆名、属性名和枚举成员名每个版本都相同，变化的只有字段号和 cmdId。同一混淆种子的新客户端按下面的步骤更新，命令都从仓库根目录执行。

1. 用 [DemiurgeCSharp](https://github.com/Mar7thDev/DemiurgeCSharp) 对新客户端的 `GameAssembly.dll` 和 `global-metadata.dat` 做静态 dump（`demiurge proto`），得到新版本的原始定义。同时准备好当前版本的原始 dump：重新导出，或从版本分支取（4.8 的在 `4.8` 分支的 `Proto/4.8/dumper/Raw/StarRail.proto`）。
2. 把当前文件里审定过的名称搬到新 dump 上，结果写回 `Proto/StarRail.proto`：

   ```sh
   dotnet run --project Proto/Tool/ProtoOrganizer -c Release -- carry \
     --dump <新版本 dump> --prev-dump <当前版本 dump> \
     --report <搬运报告的输出路径> \
     --label "Demiurge static dump | Game Version: <版本> (<客户端构建>, GameAssembly link <日期>)"
   ```

3. 重新生成命令号（上一节的 `cmdids`），构建整个解决方案并运行回归。

`carry` 只按混淆身份搬运名称：类型按混淆名，字段按属性名，枚举成员按成员名；字段号与 cmdId 一律取自新 dump，不会沿用旧数值。

- 开始前先用当前版本的 dump 重建当前的 `StarRail.proto`，定义部分必须与磁盘上的文件逐行一致才继续。文件里若有不是纯改名的手工改动（增删字段、改号、改类型），会在这一步报错，而不是被悄悄丢掉。
- 当前文件没有收录的类型（`TrackInfo` 等非协议类型）继续不收录；新类型保留 dump 给出的名称；混淆名变了但成员名与数值完全相同的枚举视为同一个枚举，报告中逐条列出。
- 客户端改了属性名的字段无法按身份跟随，在报告里表现为同一消息下一条 `-` 配一条 `+`。有证据确认是同一字段后，写进 `overrides-manual.txt`（`OBF.PROPERTY = field_name`，类型名用 `OBF = TypeName`）并写明证据；仅凭同型不足以认定。
- 没有命中任何定义的覆盖条目会报错。上一版本留下的条目在搬运前删掉，它们给出的名称已经在 `StarRail.proto` 里。
- 命令枚举的成员每次按新 dump 的命令号重新命名，不沿用上一版本的名称。

升级时一并更新：

- `GameConstants.GAME_VERSION`，写客户端自报的版本号。
- `LoginSceneRegression` 中按版本写死的 cmdId 与字段号断言（VideoKeyInfo、SceneMapInfo、SyncServerSceneChangeNotify、CommonRogueQuery 分支、快速升级、常用队伍、经典模拟宇宙 18xx 命令）。

不能把旧版本的 cmdId 数值写进新版本，也不能仅凭同型字段认定映射。

## 4.8 协议备注

当前定义来自 OSBETAWin4.7.51 客户端的 Demiurge 静态 dump（`BinaryVersion.bytes` 标注 `EngineReleaseV4.8`，GameAssembly 链接于 2026-11-05）。协议版本按测试客户端对应的正式版本命名：4.6.5x 客户端对应 4.7，4.7.5x 客户端对应 4.8；`GameConstants.GAME_VERSION` 写客户端自报的 `4.7.51`。

4.7 -> 4.8 的变化（完整记录见 `4.8` 分支的 `Proto/4.8/migration-report.txt`）：2717 个 cmdId 中 2685 个换号，4040 条消息字段重新编号，新增 63 条消息与 4 个枚举（均为混淆名，含两组新命令目录：`MFNJLCNMBON` 对应邦布联动 `RPG.Client.ActivityBangboo`，`LDJDDDDPLLH` 对应骰子收容 `RPG.Client.DiceyDungeon`，后者的消息与字段对照见[骰子收容](dicey-dungeon.zh-CN.md)），26 条消息形状变化，集中在 GridFight。需要服务端跟进的有三处：

- 踢出类型枚举的混淆名由 `JNFDGPDHENJ` 变为 `GGHJCJPPEFE`，成员与数值不变。
- `GridFightSyncData` 承载 `GridFightGameItemInfo` 的 oneof 成员由属性 `GOOCHGKBIPN` 换成 `JDGIAMABEME`，按 `overrides-manual.txt` 的证据沿用 `game_data_item_info`，尚未经客户端实机确认。
- `GridFightPermanentGameInfo` 去掉了「最后完成的教学关」`ENECKKGPKHI`，`GridFightLastTutorialFinishScNotify` 一并删除；新增的 bool `PMFHAPIFFPO` 含义未确认，服务端不填。

快速升级是命令枚举名与消息类型名不一致的例子：4.6 的 `KELLLHMHFHL`（376）对应消息 `EKLIDIDKFHC`，捕获的响应为 `PMEMIDKDAFG`（357，枚举 `MILFFHPMLMN`）；同一身份在 4.7 为 355/341，4.8 为 332/395。4.6 官服抓包 seq `36813-36829` 可验证请求/响应配对与升级费用，不能将其旧命令号直接用于后续版本。

光锥快速升级的身份为 `EOBJKAMNNPI → AMNBMKEMMFL`。4.6 原始定义重复误标为 `PromoteEquipmentCsReq/ScRsp`；官服 seq `36852/36859/36927` 的原始消息分别可解出目标等级、唯一 ID、经验消耗、突破消耗及突破等级。4.7 起映射为 `EquipmentQuickUpCsReq/ScRsp`（4.7 为 513/517，4.8 为 548/529）：`IGOOJKMPMDC` 为目标等级，`KLMIBJIKIBB` 为目标突破，`BOBOKJBAMNB` 为经验材料或光锥狗粮，`IDGEEENIMMK` 为突破材料。按属性身份使用现有定义，不改成仅执行突破的 `PromoteEquipmentCsReq`。
