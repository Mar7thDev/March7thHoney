# 从客户端导出重建资源缓存

没有同版本的完整原始资源、只有客户端时，用 [DemiurgeCSharp](https://github.com/Mar7thDev/DemiurgeCSharp) 的 `res` 命令导出客户端 DesignData，再用现有缓存补齐导出缺失的部分，得到新的 `Resource.bin`。全程在隔离目录进行：不读取玩家数据库，不写入 `Config/`，不覆盖现有缓存。导出目录放在已被 Git 忽略的 `artifacts/` 下。

原始资源完整时不需要这个流程，按 [原始资源加载回归](../testing/resource-regression.md) 的 `--cache-out` 直接生成。

## 1. 导出

```powershell
demiurge res all --game <客户端根目录> --out artifacts/<名称>/Resources --reports artifacts/<名称>/reports
```

客户端根目录是含 `GameAssembly.dll` 的那一层。`reports/` 里每个资源块一份 TSV，状态列为 `ok`、`partial`、`fail`、`empty`、`noroot`。

## 2. 补充路径名单

客户端的资源索引只保存路径哈希，dumper 要拿到路径才能给资源命名，内置名单不含新版本的路径，这些资源会被当成无名资源跳过。[`scripts/ResourceNameHarvest/`](../../scripts/ResourceNameHarvest/) 从已有导出里收集候选路径：导出文件里引用到的所有资源路径、服务端加载的全部表名、任务表里每个主线任务的 `MissionInfo` 路径、`MazePlane` 里每个位面列出的楼层文件路径（没有任何文件引用楼层路径，只能按 `P<位面>_F<楼层>` 推出；楼层文件再引用它的各个组）。dumper 对候选逐个求哈希，只采用索引里真实存在的，所以猜错没有代价。

```powershell
demiurge res keys --game <客户端根目录> > artifacts/<名称>/names/keys-0.tsv
dotnet run --project scripts/ResourceNameHarvest/ResourceNameHarvest.csproj -c Release -- artifacts/<名称>/names/keys-0.tsv artifacts/<名称>/Resources artifacts/<名称>/names/paths-1.txt
demiurge res extract --game <客户端根目录> --out artifacts/<名称>/Resources --reports artifacts/<名称>/reports --namemap artifacts/<名称>/names/paths-1.txt
demiurge res keys --game <客户端根目录> --namemap artifacts/<名称>/names/paths-1.txt > artifacts/<名称>/names/keys-1.tsv
```

新导出的文件会引用更多路径，所以用新的 `keys` 列表再收集、再导出，直到 `keys` 报告的命名数量不再增加。`--namemap` 会替换内置名单，工具因此把已命名的路径一并写回名单。

## 3. 构建并补缺

[`scripts/ResourceCacheFill/`](../../scripts/ResourceCacheFill/) 用生产加载器在隔离进程里加载导出目录，再从现有缓存补齐：

```powershell
dotnet run --project scripts/ResourceCacheFill/ResourceCacheFill.csproj -c Release -- artifacts/<名称>/Resources Config/Resource.bin artifacts/<名称>/Resource.new.bin artifacts/<名称>/fill-report
```

- 导出里存在但解析失败的表会让工具直接报错退出：这是加载器需要修的问题（例如新枚举成员），不能用旧缓存盖过去。缺失文件只产生告警，记入报告。
- 字典按键补行：导出有的行以导出为准，导出没有而旧缓存有的行补入。列表没有键，只在导出为空时整表取旧缓存。单个对象和数值只在导出保持默认值时取旧缓存。
- GridFight 的表一律只用导出，不从旧缓存补行，旧缓存多出的行数在报告里列出。`GridFightRuntimeDataSnapshot` 里因 MemoryPack 成员上限寄放的 Rogue、Daily、MainMissionSchedule 表不受此限。
- 输出必须是不存在的新文件；写入后重新读取并核对每个集合的条数。
- `fill-report.txt` 列出加载告警、每个成员的导出行数 / 旧缓存行数 / 补入行数，以及补缺前后「起止子任务未注册」的主线任务数量。

## 4. 验证、替换与回滚

```powershell
dotnet run --project scripts/GridFightRegression/GridFightRegression.csproj -- --cache artifacts/<名称>/Resource.new.bin
dotnet run --project scripts/LoginSceneRegression/LoginSceneRegression.csproj -c Release -- --resource-cache artifacts/<名称>/Resource.new.bin
dotnet run --project scripts/DailyActiveRegression/DailyActiveRegression.csproj -c Release -- --cache artifacts/<名称>/Resource.new.bin
```

回归通过后备份现有 `Config/Resource.bin`，再把新文件复制过去。回滚时把备份复制回来；枚举取值变化过的版本需要程序和缓存一起恢复。

## OSBETAWin4.7.51 的结果（2026-11-14）

- 导出：索引 144 867 个资源，内置名单命名 130 581 个，补名收敛后 134 450 个，写出 133 541 个 JSON，其中楼层 710 个；仍有 10 417 个资源没有路径。
- 解不出的表 22 张：`MechCraft*` 21 张和 `PetConfig`（dumper 报 `UnknownFieldType`，行类型无法识别），`DecideAvatarOrder` 在客户端里是空表。`LocalPlayer_Pearl_00`、`Nihilux_10`、`Ellen_00`、`Astra_00` 等 10 个新角色的迷宫技能文件不在索引里。
- 加载器改动：`GridFightBonusTypeEnum.AllAvatarByRarity = 25`、`GridFightTraitEffectTypeEnum.GainOtherRoleTrait = 34` 是新表用到的新成员，数值与客户端一致；其余成员不动。
- 补缺：从 4.6.52 缓存补入 59 个成员共 7 304 行（MechCraft、宠物、消息、奖励、子任务、57 个楼层等）。起止子任务未注册的主线任务由 179 个降到 18 个：1054800–1054804、3050321、4010263–4010265、4011900、5000514、8048117–8048121、8048208、8048209。
- 楼层路径：最初的名单没有按位面推导楼层路径，最新一期虚构叙事 `20294` 的楼层 `P30148_F30148002` 因此没有导出，`LoginSceneRegression` 报「PF 20294 starts on stage 1」失败。加入这类候选后多导出 13 个楼层和它们引用的组，该项通过。
- GridFight：新表把阿哈的角色 ID 由 `151102` 改为 `151112` / `151122` / `151132`，并调整了投资列表等内容，服务端第二赛季的适配见 [GridFight 对比与回归](../testing/grid-fight-regression.md)。
- 新缓存上的回归：GridFightRegression 557/557，LoginSceneRegression 698/0，DailyActiveRegression 76/0。
- 缓存要用与程序同一版本的代码生成：场景、任务等加载器新增字段后，旧程序生成的缓存里这些字段是默认值。本次的缓存、报告和替换前的 4.6.52 缓存都留在生成机器的 `artifacts/resources-4.7.51/` 下，不随仓库分发。均未经客户端实机验证。