# Proto

Source: [`Proto/`](../../Proto/)

按游戏版本保存、按 `Cmd*Type` 分组的 protobuf 定义。当前活动版本为 **4.8**，`Proto.csproj` 在构建时把该版本的顶层 `*.proto` 自动生成为 `obj/<configuration>/<framework>/protobuf/4.8/` 中的 C#，再编译；不再跟踪生成的 C#。

## 目录

```text
Proto/
  <version>/
    dumper/                    Mar7thDumper 自有产物及整理后的 proto
      Raw/                     Dumper 原始 StarRail.proto、packetIds、translations 和日志
      Cmd<Xxx>Type.proto       按命令模块分组的当前版本定义
      Common.proto             跨模块共享类型
      Orphans*.proto           尚未归入命令闭包的类型
      CmdIds.cs                当前版本 cmdid 快照
    reference/                 可选的外部 reference，只在对应版本真实存在时保存
    compat-types.txt           进门阶段暂从上一版本保留的 API 类型
    cmdid-aliases.txt          旧服务端命令名到当前 cmdid 名的兼容别名
  Build/                       MSBuild 自动生成目标
  Archives/                    旧版协议无损 ZIP（迁移时按需恢复）
  obj/.../protobuf/4.8/        活动 C#，构建时生成且 Git 忽略
  Tool/ProtoOrganizer/         版本迁移、分组、cmdid 和 C# 生成工具
```

`Proto/4.4/` 是迁移到上述布局之前的兼容基线：根目录 grouped proto 和 `Raw/` 仍保留，`Proto/4.4/dumper/Raw/` 保存自有 Dumper 原始产物。`Proto/4.5/` 不创建虚假的 reference；在 4.5 reference 实际可用前，只使用 Dumper 结果、同混淆种子的 4.4 对齐信息和显式兼容清单。

资源提取与 Honey 资源缓存是两个边界：Mar7thDumper 仍执行包括原始资源导出在内的完整流程；ProtoOrganizer 只消费 proto/cmdid 产物，不会写 `Config/Resource.bin` 或切换 Honey 的资源目录。

## 历史 4.5 迁移示例（不是本分支的活动版本）

先按 [归档说明](../../Proto/Archives/README.md) 解压 4.4 和 4.5；生成到 `Proto/Generated` 仅供工具检查，服务器构建不会编译这个旧路径。不要用本示例替换当前命令号或活动版本。

```sh
dotnet run --project Proto/Tool/ProtoOrganizer/ProtoOrganizer.csproj -- \
  --proto-file Proto/4.5/dumper/Raw \
  --raw Proto/4.5/dumper/Raw \
  --out Proto/4.5/dumper \
  --same-seed-raw Proto/4.4/dumper/Raw \
  --same-seed-proto Proto/4.4 \
  --translation Proto/4.5/translations.txt \
  --compat-types Proto/4.5/compat-types.txt \
  --cmdid-aliases Proto/4.5/cmdid-aliases.txt \
  --version 4.5 --active --compile \
  --cs-out Proto/Generated \
  --cmdids-out Proto/4.5/dumper/CmdIds.cs \
  --cmdids-out KcpSharp/CmdIds.cs
```

工具会依次完成：读取 Dumper 原始定义与 `Cmd*Type` enum、恢复 cmdid、利用同种子旧版本对齐类型和字段、加入版本化兼容类型/命令别名、重新分组、运行 `protoc`。当前活动版本必须达到全部 proto 编译成功后才会用于 Honey 构建。

已有 grouped proto 只需补语义名时，使用 `--rewrite-proto` 指定原位翻译文件、使用 `--compile-rewrite` 指定需要覆盖生成的同名 C# 文件。该模式只应用翻译表中以 `!` 开头的类型映射，不引入 Raw 中的其他定义；可同时通过 `--cmdid-aliases` 与 `--cmdids-out` 更新语义命令别名。

`protoc` 默认从 `Google.Protobuf.Tools` NuGet 缓存按当前系统自动定位，也可用 `--protoc <path>` 覆盖。

## 自动生成与验证

```sh
dotnet restore March7thHoney.sln
dotnet build March7thHoney.sln -c Release --no-restore
python3 scripts/test-proto-generation.py
```

NuGet 固定 `Google.Protobuf.Tools 3.33.1`（protoc 33.1），与现有 `Google.Protobuf 3.33.1` 运行时一致；两个版本都在仓库根目录的 `Directory.Packages.props` 中声明。首次构建需要还原此工具包；工具不会进入服务器发布产物。Linux、Windows 使用宿主平台工具，交叉发布不改变宿主选择。该版本 macOS 工具为 x64，Apple Silicon 需要 Rosetta；也可用 `-p:ProtocolCompiler=<path>` 指向版本严格匹配的 protoc 33.1。

目标检测 schema 的新增、删除、修改和缺失的生成文件；无变化时跳过生成。`dotnet clean` 后再次构建会恢复全部生成代码。修改分组定义，不手改 `obj/`。

构建前的检出体积明显减少，但构建时生成代码和 NuGet 缓存仍占空间。此改动不删除旧 Git 历史；完整克隆仍可能下载历史大文件。

## 4.7 版本

4.7 的原始定义来自 [DemiurgeCSharp](https://github.com/Mar7thDev/DemiurgeCSharp) 对客户端 `GameAssembly.dll` + `global-metadata.dat` 的静态 dump（`demiurge proto`），不再依赖运行时 dumper。

```text
Proto/4.7/
  dumper/Raw/StarRail.proto    Demiurge 原始输出：混淆类型名（恢复出的真名带 // obf:）、4.7 字段号与 cmdId
  Raw/StarRail.proto           翻译后的扁平 master，分组文件由它生成
  Raw/packetIds.txt            cmdid -> 名称（// obf: 原混淆名）
  Raw/translations.txt         obf 类型 -> 名称
  overrides-manual.txt         4.7 逐条审定的类型名，附证据
  migration-report.txt         4.6 -> 4.7 迁移记录：新增、删除、证据分歧
  cmdid-aliases.txt            4.6 别名按 cmdid -> obf 跟随到 4.7 名称
```

迁移的依据是混淆身份在版本间稳定：类型的混淆名、属性名和 `XxxFieldNumber` 常量名每个版本都相同，变化的只有字段号、cmdId 和 backing field 名。Demiurge 因此按属性名给字段命名，4.6 已审定的类型名、字段名和枚举成员名按混淆身份逐个搬到 4.7 的字段号上。字段身份按证据强弱逐级匹配：4.6 手工钉死的字段与原样保留的混淆名 → 4.4/4.5 Mar7thDumper 常量名与同版本可读定义按字段号的对应 → 4.6 master 同字段号 → 4.6 自动 overrides → 4.6 dumper raw（原名或 `nt.txt` 译名）。`nt.txt` 来自按存储槽位匹配，相邻同类型字段会互换（SceneInfo 的 `world_id` / `leader_entity_id`、`lighten_section_list` / `opened_chests_list` 即此类），只用来识别身份，不单独决定名称。

4.7 已不是活动版本，其定义作为 4.8 的名称来源保留在工作树里，下一次切换活动版本后再按 [归档说明](../../Proto/Archives/README.md) 归档。分组文件由 `Raw/StarRail.proto` 生成，修改定义时改 master 而不是分组后的 `Cmd*Type.proto`：`GridFightGameBasicInfo` 的五个字段名曾只改在分组文件里，master 已按分组文件补齐。

## 4.8 活动版本

4.8 来自 OSBETAWin4.7.51 客户端的 Demiurge 静态 dump（`BinaryVersion.bytes` 标注 `EngineReleaseV4.8`，GameAssembly 链接于 2026-11-05）。版本目录按该测试客户端对应的正式版本命名：4.6.5x 客户端对应 `Proto/4.7`，4.7.5x 客户端对应 `Proto/4.8`；`GameConstants.GAME_VERSION` 写客户端自报的 `4.7.51`。

```text
Proto/4.8/
  dumper/Raw/StarRail.proto    Demiurge 原始输出
  Raw/StarRail.proto           由 4.7 master 搬运名称得到的扁平 master，分组文件由它生成
  Raw/packetIds.txt            cmdid -> 名称（// obf: 原混淆名）
  Raw/translations.txt         obf 类型 -> 名称
  overrides-manual.txt         搬运无法按身份跟随的逐条审定，附证据
  migration-report.txt         4.7 -> 4.8 搬运报告：新增、删除、形状变化
  cmdid-aliases.txt            4.7 的别名原样沿用；别名指向常量名而非数值
```

同一混淆种子的新客户端分两步更新。第一步把上一版本审定过的名称搬到新 dump 上：

```sh
dotnet run --project Proto/Tool/ProtoOrganizer/ProtoOrganizer.csproj -- \
  --carry-dump Proto/4.8/dumper/Raw/StarRail.proto \
  --carry-prev-dump Proto/4.7/dumper/Raw/StarRail.proto \
  --carry-prev-master Proto/4.7/Raw/StarRail.proto \
  --carry-overrides Proto/4.8/overrides-manual.txt \
  --carry-out Proto/4.8/Raw --carry-report Proto/4.8/migration-report.txt \
  --carry-label "Demiurge static dump | Game Version: 4.8 (OSBETAWin4.7.51, GameAssembly link 2026-11-05)"
```

`--carry-dump` 只按混淆身份搬运名称：类型按混淆名，字段按属性名，枚举成员按成员名；字段号与 cmdId 一律取自新 dump，不会沿用旧数值。

- 开始前先用上一版本的 dump 重建上一版本的 master，必须与磁盘上的文件逐行一致才继续。master 里若有不是纯改名的手工改动（增删字段、改号、改类型），会在这一步报错，而不是被悄悄丢掉。
- 上一版本 master 没有收录的类型（`TrackInfo` 等非协议类型）继续不收录；新类型保留 dump 给出的名称；混淆名变了但成员名与数值完全相同的枚举视为同一个枚举，报告中逐条列出。
- 客户端改了属性名的字段无法按身份跟随，在报告里表现为同一消息下一条 `-` 配一条 `+`。有证据确认是同一字段后，写进 `overrides-manual.txt`（`OBF.PROPERTY = field_name`，类型名用 `OBF = TypeName`）并写明证据；仅凭同型不足以认定。没有命中任何定义的覆盖条目会报错。

第二步重建分组定义和命令号。此后维护 4.8 定义时修改 `Raw/StarRail.proto`，再执行这一步。`--cs-out Proto/Generated` 是可选的工具校验输出，已经忽略且不参与服务器编译；随后必须正常构建，让 MSBuild 从活动分组定义自动生成到 `obj/`：

```sh
dotnet run --project Proto/Tool/ProtoOrganizer/ProtoOrganizer.csproj -- \
  --proto-file Proto/4.8/Raw --raw Proto/4.8/Raw --out Proto/4.8 \
  --version 4.8 --active --compile --cs-out Proto/Generated \
  --cmdid-aliases Proto/4.8/cmdid-aliases.txt --compat-types Proto/4.8/compat-types.txt \
  --cmdids-out Proto/4.8/CmdIds.cs --cmdids-out KcpSharp/CmdIds.cs
```

切换活动版本时把 `Proto.csproj` 的 `ActiveProtocolVersion` 改为 `4.8`。

4.7 -> 4.8 的变化（详见 `migration-report.txt`）：2717 个 cmdId 中 2685 个换号，4040 条消息字段重新编号，新增 63 条消息与 4 个枚举（均为混淆名，含两组新命令目录：`MFNJLCNMBON` 对应邦布联动 `RPG.Client.ActivityBangboo`，`LDJDDDDPLLH` 对应骰子收容 `RPG.Client.DiceyDungeon`，后者的消息与字段对照见 [骰子收容](dicey-dungeon.md)），26 条消息形状变化，集中在 GridFight。需要服务端跟进的有三处：

- 踢出类型枚举的混淆名由 `JNFDGPDHENJ` 变为 `GGHJCJPPEFE`，成员与数值不变。
- `GridFightSyncData` 承载 `GridFightGameItemInfo` 的 oneof 成员由属性 `GOOCHGKBIPN` 换成 `JDGIAMABEME`，按 `overrides-manual.txt` 的证据沿用 `game_data_item_info`，尚未经客户端实机确认。
- `GridFightPermanentGameInfo` 去掉了「最后完成的教学关」`ENECKKGPKHI`，`GridFightLastTutorialFinishScNotify` 一并删除；新增的 bool `PMFHAPIFFPO` 含义未确认，服务端不填。

`LoginSceneRegression` 中有按版本写死的 cmdId 与字段号断言（VideoKeyInfo、SceneMapInfo、SyncServerSceneChangeNotify、CommonRogueQuery 分支、快速升级、常用队伍、经典模拟宇宙 18xx 命令），切换活动版本时一并更新。

命令枚举名不一定等于 protobuf 消息类型名。快速升级例如：4.6 的 `KELLLHMHFHL`（376）在 `dumper/Raw/cmdid-registry.txt` 中对应 `EKLIDIDKFHC`，捕获的响应为 `PMEMIDKDAFG`（357，枚举 `MILFFHPMLMN`）；4.7 的 `Raw/packetIds.txt` 和同身份定义分别对应 355/341，4.8 为 332/395。处理器绑定当前命令常量并解析真实消息类型；仅按枚举名搜索同名 `message` 会漏掉现有结构。4.6 官服抓包 seq `36813-36829` 可验证请求/响应配对与升级费用，不能将其旧命令号直接用于 4.7。

光锥快速升级的身份为 `EOBJKAMNNPI → AMNBMKEMMFL`。4.6 原始定义重复误标为 `PromoteEquipmentCsReq/ScRsp`；官服 seq `36852/36859/36927` 的原始消息分别可解出目标等级、唯一 ID、经验消耗、突破消耗及突破等级。4.7 已映射为 `EquipmentQuickUpCsReq/ScRsp`（513/517，4.8 为 548/529）：`IGOOJKMPMDC` 为目标等级，`KLMIBJIKIBB` 为目标突破，`BOBOKJBAMNB` 为经验材料或光锥狗粮，`IDGEEENIMMK` 为突破材料。按属性身份使用现有活动定义，不改成仅执行突破的 `PromoteEquipmentCsReq`。请求没有处理器时框架自动发送空成功响应，不能用该响应判断升级已执行。

## 新版本约束

- 自有 Dumper 产物始终放入该版本的 `dumper/Raw/`，不可混入 reference。
- `reference/` 只保存同一游戏版本的参考定义，未更新时保持不存在或保持旧版本目录，不得伪造新版本 reference。
- 兼容类型和命令别名必须放在对应版本清单中，禁止把旧 cmdid 数值覆盖到新版本。
- 切换活动版本后同步更新 `Proto.csproj` 的 `ActiveProtocolVersion`、分组定义和 `KcpSharp/CmdIds.cs`，再执行 `dotnet build March7thHoney.sln` 与生成回归。
