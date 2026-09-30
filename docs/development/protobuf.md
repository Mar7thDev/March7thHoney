# Proto

Source: [`Proto/`](../../Proto/)

按游戏版本保存、按 `Cmd*Type` 分组的 protobuf 定义。仓库可以同时保留多个版本，但只有一个版本会生成到 `Proto/Generated` 并参与构建。

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
  Generated/                   活动版本生成的 C#，由 Proto.csproj 编译
  Tool/ProtoOrganizer/         版本迁移、分组、cmdid 和 C# 生成工具
```

`Proto/4.4/` 是迁移到上述布局之前的兼容基线：根目录 grouped proto 和 `Raw/` 仍保留，`Proto/4.4/dumper/Raw/` 保存自有 Dumper 原始产物。`Proto/4.5/` 不创建虚假的 reference；在 4.5 reference 实际可用前，只使用 Dumper 结果、同混淆种子的 4.4 对齐信息和显式兼容清单。

资源提取与 Honey 资源缓存是两个边界：Mar7thDumper 仍执行包括原始资源导出在内的完整流程；ProtoOrganizer 只消费 proto/cmdid 产物，不会写 `Config/Resource.bin` 或切换 Honey 的资源目录。

## 生成 4.5 活动版本

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

## 4.7 活动版本

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

维护 4.7 定义时修改 `Raw/StarRail.proto`，再生成：

```sh
dotnet run --project Proto/Tool/ProtoOrganizer/ProtoOrganizer.csproj -- \
  --proto-file Proto/4.7/Raw --raw Proto/4.7/Raw --out Proto/4.7 \
  --version 4.7 --active --compile --cs-out Proto/Generated \
  --cmdid-aliases Proto/4.7/cmdid-aliases.txt --compat-types Proto/4.7/compat-types.txt \
  --cmdids-out Proto/4.7/CmdIds.cs --cmdids-out KcpSharp/CmdIds.cs
```

`LoginSceneRegression` 中有按版本写死的 cmdId 与字段号断言（SceneMapInfo、SyncServerSceneChangeNotify、经典模拟宇宙 18xx 命令），切换活动版本时一并更新。

## 新版本约束

- 自有 Dumper 产物始终放入该版本的 `dumper/Raw/`，不可混入 reference。
- `reference/` 只保存同一游戏版本的参考定义，未更新时保持不存在或保持旧版本目录，不得伪造新版本 reference。
- 兼容类型和命令别名必须放在对应版本清单中，禁止把旧 cmdid 数值覆盖到新版本。
- 切换活动版本后必须重新生成 `Proto/Generated`、`KcpSharp/CmdIds.cs`，并执行 `dotnet build March7thHoney.sln`。
