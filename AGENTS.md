# 项目协作规范

本文件为 March7thHoney 的贡献者和自动化开发助手提供项目结构、开发约定与验证入口，适用于整个仓库。子目录存在更具体的 `AGENTS.md` 时，结合其说明开展工作。文档中的版本示例可能落后于代码，实际行为以当前源码、项目文件和资源为准。

## 协作原则

- 修改前阅读相关实现、调用方和文档，明确预期行为、影响范围及兼容要求。
- 开始修改前查看 `git status --short`，保留工作区已有改动。保持改动聚焦，避免夹带无关重构或全仓库格式化。
- 区分已确认的行为、实现假设和待验证事项；影响接口、数据兼容或需求范围的不确定性应在实现前澄清。
- 提交说明应概括解决的问题、行为变化和验证结果，便于独立审查。Git 身份、分支及推送目标由贡献者所在环境和协作流程决定，不在项目规范中固定。
- 涉及数据迁移、配置替换或部署的变更应提供操作前提、备份与回滚说明。
- 交付时列出实际执行的检查及未覆盖的部分，区分编译成功、服务端回归通过与客户端实机验证。

## 项目结构

项目以 C# 和 .NET 10 为主，解决方案为 `March7thHoney.sln`；Roslyn 源生成器 `SourceGen` 单独使用 `netstandard2.0`。

| 路径 | 职责 |
| --- | --- |
| `Program/Program/EntryPoint.cs` | 启动入口，协调配置、资源、数据库、网络服务和关闭流程 |
| `GameServer/Game/` | 玩家、场景、战斗、任务和各玩法的管理器与组件 |
| `GameServer/Server/Packet/Recv/` | 客户端请求处理器，按功能分目录 |
| `GameServer/Server/Packet/Send/` | 服务端响应与通知包 |
| `WebServer/` | HTTP 服务、登录鉴权、MUIP、后台和玩家控制台页面 |
| `Command/Command/Cmd/` | 游戏与管理命令实现 |
| `Common/` | 共享配置、资源模型、数据库实体、枚举、国际化和工具 |
| `SourceGen/` | 包处理器、命令、任务、数据库及 MemoryPack 注册代码生成 |
| `KcpSharp/` | KCP 传输及活动协议命令号 `CmdIds.cs` |
| `Proto/<version>/` | 各游戏版本的协议来源、分组定义和兼容清单 |
| `Proto/Generated/` | 当前活动版本生成的 C# 协议代码 |
| `Proto/Tool/ProtoOrganizer/` | 协议整理、版本迁移和生成工具 |
| `Config/` | 自定义 JSON、语言包、资源缓存及本地运行数据；部分内容受版本控制 |
| `Resources/`、`TurnBasedGameData/` | 本地游戏资源，已被 Git 忽略，干净检出不保证存在 |
| `scripts/` | 独立回归程序、缓存升级工具及 Linux 部署脚本 |
| `docs/` | 开发、测试、API、入门和运维文档，入口为 `docs/README.md` |

## 环境、构建与启动

需要 .NET SDK 10 和 Git。以下命令均从仓库根目录执行；Linux 路径区分大小写，使用 `Program`。

```sh
dotnet restore March7thHoney.sln
dotnet build Program/Program.csproj -c Release --no-restore
```

修改共享项目、源生成器或协议后，验证整个解决方案：

```sh
dotnet build March7thHoney.sln -c Release --no-restore
```

需要启动完整服务时，先确认本地配置和配套资源，再执行：

```sh
dotnet run --project Program/Program.csproj
```

完整服务会初始化运行配置、日志和数据库。日常回归优先使用下文的隔离测试程序。根目录还提供 `Start.bat`、`Start.sh` 和 `Start.command`；部署步骤见 `docs/getting-started/native-linux.md`。

NativeAOT 发布使用项目定义的 `-p:DoAot=true`，不要全局传入 `-p:PublishAot=true`，后者会错误作用于 `netstandard2.0` 的源生成器。仅在涉及发布或 AOT 兼容性时执行对应平台发布验证。

## 开发规范

- 遵循修改文件附近的 C# 风格，通常为四空格缩进、文件范围命名空间和 PascalCase 类型/方法名。保留 nullable 检查，不为消除告警随意添加 `!` 或关闭检查。
- 玩家功能按既有管理器或组件扩展，避免持续堆入 `PlayerInstance`。新增组件应核对初始化和注册流程，参考 `docs/development/player-components.md`。
- 请求处理器沿用 `[Opcode(CmdIds.XxxCsReq)]` 和相邻处理器的基类，响应包放入对应的 `Send` 目录。业务状态变化放入所属玩法管理器，协议组装与业务逻辑保持清晰边界。
- 命令沿用 `[CommandInfo]`、`[CommandDefault]` 等声明及现有权限检查；任务处理器参考 `docs/development/mission-system.md`。不要绕过源生成注册流程另建反射扫描机制。
- `Common/Enums/` 中新增枚举以 `Enum` 结尾，遵循 `docs/development/enum-conventions.md` 的命名空间约定。
- 使用现有异步调用链并等待必要操作，避免在请求处理中使用 `.Result`、`.Wait()` 或无生命周期管理的后台任务。
- 涉及奖励、购买、结算、账号操作时，先验证输入与权限，再提交状态；检查重复请求、失败路径和并发操作，防止重复发奖或部分写入。
- 修改存档、数据库或 MemoryPack 资源模型时验证旧数据兼容性，不能仅凭新数据可用就认定升级安全。
- 保持 AOT 兼容：新增 Web JSON 请求/响应类型时检查 `WebServer/WebJsonContext.cs` 及相关序列化上下文；修改生成规则应改 `SourceGen/` 源码，不手改 `obj/` 里的生成文件。
- 面向用户的命令文案沿用 `I18NManager` 与 `Config/Languages/`，页面文案遵循所属页面的国际化机制。API 或配置行为变化应同步相关文档。
- 跨平台文件操作使用合适的路径 API，避免硬编码开发机绝对路径；保留原有文件编码和换行风格。

## 协议与资源约束

- 协议流程见 `docs/development/protobuf.md`。执行其中的版本示例前，核对当前活动协议、版本目录和兼容清单，不直接照抄旧版本号。
- `Proto.csproj` 只编译 `Proto/Generated/**/*.cs`。协议修复应落到对应版本的定义、翻译映射、兼容清单或生成工具，再重新生成，避免只改生成的 C#。
- 切换活动协议时同步生成 `Proto/Generated/` 与 `KcpSharp/CmdIds.cs`，完成整个解决方案构建。不能用旧版本命令号覆盖新版本，也不能仅依据同型字段猜测协议映射。
- Dumper 原始产物保留在对应版本的 `dumper/Raw/`；`reference/` 只能存放确实存在的同版本参考定义。
- `GameServer.csproj` 存在显式排除的协议处理器。重新启用前应解决其协议依据和兼容性问题，不直接删除排除项来恢复编译。
- 协议生成与资源缓存更新是独立流程；运行 ProtoOrganizer 不代表 `Config/Resource.bin` 已更新。
- 排查资源或存档问题时优先使用副本和隔离环境，保留原始数据。缓存升级参考 `docs/operations/scene-cache-upgrade.md`，输出到新文件，验证并备份后再替换；保留程序与缓存配套回滚能力。
- `Config.json`、玩家数据库、日志、令牌和本地资源不应混入开发提交。以 `.gitignore` 和 `git ls-files` 核对边界，不将整个 `Config/` 视为可丢弃目录。

## 测试规范

现有回归是 `scripts/` 下的独立可执行项目，未包含在主解决方案中。`dotnet build March7thHoney.sln` 或单独执行 `dotnet test` 不能替代这些回归；使用 `dotnet run --project` 运行。首次运行允许其自行还原依赖，不随意加 `--no-restore`。

按改动选择相关命令；涉及多个模块或公共行为时组合执行：

| 改动范围 | 回归命令 | 前提与说明 |
| --- | --- | --- |
| 账号、鉴权、封锁、令牌、删除及数据库并发 | `dotnet run --project scripts/AuthRegression/AuthRegression.csproj` | 临时 SQLite 与测试配置；预期 `Auth regression failures: 0` |
| Web 后台、MUIP、命令目标隔离及异步输出 | `dotnet run --project scripts/WebServerRegression/WebServerRegression.csproj` | 独立内存数据与临时目录；预期 `Regression failures: 0` |
| 登录协议、RSA、场景加载与传送 | `dotnet run --project scripts/LoginSceneRegression/LoginSceneRegression.csproj -c Release` | 临时数据库和本机 HTTP 服务；场景用例读取当前 `Config/Resource.bin` 的副本 |
| GridFight 玩法与状态同步 | `dotnet run --project scripts/GridFightRegression/GridFightRegression.csproj` | 读取当前 `Config/Resource.bin`，不初始化玩家数据库，不写回缓存 |

- 具体覆盖和预期日志见 `docs/testing/` 下对应文档。以进程退出码和最终失败汇总判断结果；模拟异常会产生预期错误日志。
- 行为修复优先在对应回归程序中增加能复现问题的检查，验证外部结果、状态变化或消息内容；根据问题覆盖重复请求、非法输入、空数据和兼容路径。
- 纯文档、注释或低影响的格式调整只需检查内容、路径和差异，无需增加测试或运行完整服务。
- 缺少资源、SDK 或其他前提时明确记录阻碍，不伪造资源、不改用真实玩家数据库，也不报告未运行的检查为通过。
- Web 页面需要交互验证时，可给 `AuthRegression` 或 `WebServerRegression` 命令追加 `-- --serve`。具体本机地址和测试凭据见对应测试文档，完成后停止本次启动的测试进程。
- 页面交互变更应验证相关操作流程、成功与失败反馈及权限边界，记录测试环境和复现步骤。
- 服务端回归不能替代客户端实机测试；场景进入、UI、动画、战斗等需要实机验证的结论须有实际证据。

## 交付检查

1. 查看最终差异，确认只有任务相关修改，没有运行数据、凭据或无关生成产物。
2. 对 Git 可见的修改运行 `git diff --check`，并执行与改动范围匹配的构建和回归。
3. 涉及协议生成、配置或接口行为时，检查源码、生成结果和文档是否一致。
4. 清理本次启动的测试服务，说明修改位置、验证结果及剩余限制。

本文件纳入版本控制，项目结构、开发流程或测试入口变化时应同步更新。
