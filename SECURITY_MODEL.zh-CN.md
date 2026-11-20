# 安全模型

[English](SECURITY_MODEL.md) | 简体中文

这是 March7thHoney 安全审查和代码扫描所依据的威胁模型。报告漏洞请按照 [SECURITY.zh-CN.md](SECURITY.zh-CN.md) 操作。

本文以仓库自己的 [SECURITY.zh-CN.md](SECURITY.zh-CN.md)、[账号安全指南](docs/operations/account-security.zh-CN.md) 和 [安全回归检查](docs/testing/security-regression.zh-CN.md) 为基础。没有使用基础设施即代码仓库：本仓库的部署方式是 `scripts/` 中的 systemd/Debian 辅助脚本。

## 系统概览

一个面向回合制游戏客户端、自行托管的 .NET 10 私服。一个进程（`Program/Program/EntryPoint.cs`）通常同时运行：

- ASP.NET 最小 API 的 **WebServer**（HTTP，默认 `0.0.0.0:21000`），提供客户端 dispatch/登录、账号注册和找回、网页管理面板、MUIP、玩家控制台以及可选的网站游戏账号接口；
- 通过 KCP/UDP 使用游戏协议的 **GameServer**（默认 `0.0.0.0:23301`）。

Dispatch 和 GameServer 也可以分成独立进程运行，二者通过 `/validate_account_token` 和 `/get_account_info` 通信，用 `ServerOption.ServerConfig.ServerExchangeSecret` 认证。数据存放在 SQLite 或 MySQL 中（`Common/Database/`）。游戏资源是本地缓存（`Config/Resource.bin`），不在 Git 中。

## 组件

| 组件 | 代码 | 信任边界 |
|---|---|---|
| **Login & Accounts** | `WebServer/Controllers/DispatchRoutes.cs`、`GateServerRoutes.cs`、`SdkShopRoutes.cs`、`LogServerRoutes.cs`、`WebServer/Handler/*Login*`、`RegistrationHandler.cs`、`AccountRecoveryHandler.cs`、`ComboTokenGranterHandler.cs`、`AuthRateLimiter.cs`、`LoginCryptoHelper.cs`、`WebServer/Pages/LoginPlatform` | 互联网 → 服务器；未认证 |
| **Admin & MUIP** | `WebServer/Controllers/AdminRoutes*.cs`、`MuipServerRoutes.cs`、`WebServer/Handler/AdminSessionManager.cs`、`AdminSettingsRegistry.cs`、`AdminLogBuffer.cs`、`WebServer/Pages/Admin` | 管理密钥持有者 / 管理角色 |
| **Player Console** | `WebServer/Controllers/ConsoleRoutes.cs`、`PlayerConsoleSessionManager.cs`、`WebServer/Pages/PlayerConsole` | 已登录玩家 → 仅限自己的账号 |
| **Server Exchange & Web Account API** | `WebServer/Controllers/ServerExchangeRoutes.cs`、`WebAccountRoutes.cs`、`JsonRoutes.cs`、`HandbookRoutes.cs` | 服务之间的共享密钥 / API 密钥 |
| **GameServer** | `GameServer/`、`KcpSharp/`、`Proto/` | 通过 UDP 认证的游戏客户端；认证前的握手 |
| **Commands** | `Command/Command/`、`GameServer/Command/` | 玩家/控制台/MUIP 发送者 → 权限检查（`CommandPermissions.TargetOthers`、每个命令的 `Permission`） |
| **Persistence & Config** | `Common/Database/`、`Common/Configuration/`、`Config/` | 归运营者所有 |
| **Deployment** | `scripts/*.sh`、`scripts/update-server.sh`、`.github/workflows/ci.yml` | 运营者主机 / CI |

组件名保留英文，以便和扫描结果的 `Component` 标签对应。

## 入口以及谁能访问

- HTTP dispatch/登录/注册/忘记密码/邮箱验证：任何能访问 HTTP 端口的人（公开服务器通常就是整个互联网）。
- `/admin/api/*`：管理 Bearer 会话或 `X-Admin-Session`，用管理密钥获取（`/admin/api/login`）；基于角色的权限在 `AdminRoutes.Access.cs` 中。
- `/muip/*`：用管理密钥通过 `/muip/create_session` + `/muip/auth_admin` 获得会话，然后 `exec_cmd` 对目标 UID 执行服务器命令。
- `/player/*`、`/console/*`：账号登录后的玩家控制台会话；`exec` 以该玩家身份执行命令。
- 网站游戏账号接口（`WebAccountRoutes.cs`）：默认关闭（`ServerOption.Auth.WebAccountApi`），开启后由密钥保护。
- `/validate_account_token`、`/get_account_info`：共享的 `ServerExchangeSecret`；未设置时返回 401。
- `/sr-tools`、`/sr-tools-export`：JSON 工具路由（`JsonRoutes.cs`）——按 UID 检查密码；没有密码的账号只凭 UID 即可访问。
- KCP/UDP 游戏端口：任何人；握手准入限制（`KcpAdmissionRegression`）和来源地址绑定（`KcpSecurityRegression`）；之后是令牌认证的玩家会话，`GameServer/Server/Packet/Recv/` 中约有 70 组请求处理器。

## 资产

- 管理密钥和管理会话（完全控制服务器：命令、账号、设置、日志、轮换管理密钥）。
- 玩家账号：密码、登录/combo 令牌、邮箱地址、最近的 IP/设备 ID（个人信息）。
- 玩家游戏状态（背包、货币、进度）——在共享服务器上，其完整性关系到公平。
- 主机完整性：文件系统（日志下载 `AdminRoutes.Logs.cs`、资源重载）、进程可用性。
- `ServerExchangeSecret`、SMTP 凭据（`AccountEmailSender.cs`）、`Config.json` 中的数据库凭据。

## 攻击者画像

- **未认证的网络客户端**（HTTP 或 UDP）：滥用注册、绕过登录、伪造令牌、绕过频率限制、对 KCP 握手的拒绝服务、注入其他玩家的 UDP 会话。
- **已注册玩家**（使用游戏客户端或玩家控制台）：操作其他 UID（IDOR）、提升命令权限、通过重复或并发请求复制奖励/货币、用畸形数据包使服务器崩溃。
- **低权限管理角色的持有者**：提升为完全管理员。
- **恶意网页**，针对已登录管理员的浏览器：对管理面板和玩家控制台的 CSRF/XSS。

**维护者决定：** 谁能获得账号因运营者而异；按开放注册处理——互联网上任何人都能成为已注册玩家。以上所有攻击者画像都在范围内。

## 资产优先级

**维护者决定：** 以上所有资产同等重要，不对它们排序。

## 现有控制

- 认证频率限制（`AuthRateLimiter.cs`）、设备/IP 封禁规则、账号封禁、令牌吊销（见账号安全指南）。
- `X-Forwarded-*` 的可信代理白名单（`HttpServer.TrustedProxies`，默认只有回环地址）。
- 用登录 RSA（`Config/Security/LoginRsa*.pem`）传输密码。
- KCP：会话 ID 只用于路由；创建时绑定来源地址；握手容量限制。
- `CommandManager.cs` 中的命令权限检查。
- CI 中运行的回归：`scripts/verify-security.py`、Auth/WebServer/Kcp/SourceGen 回归。

## 范围外 / 接受的风险（来自 SECURITY.md）

- 游戏客户端及其发行商的服务。
- 需要运营者本就拥有的访问权限的问题：服务器自己的 `Config.json`、数据库或主机。
- UDP 来源检查不是密码学认证；接受来源地址伪造的风险，由防火墙缓解（security-regression.md）。
- `System.Security.Cryptography.Pkcs` 公告：已固定到修复版本，没有已知可达的入口。
- 设备 ID 由客户端提供，并非不可伪造（account-security.md）。
- **维护者决定：** 提交到仓库的登录 RSA 密钥对（`Config/Security/LoginRsaPrivateKey.pem`、`LoginRsaPublicKey.pem`，按 README 所述是与 CyreneLauncher 共用的公开测试材料）和 `Config/ClientSecretKey.ec2b` 是有意公开的：接受的风险。只报告这些已提交密钥、或依赖其保密性的发现，均属接受的风险。（`ClientSecretKey.ec2b` 缺失时由 `Common/Util/Crypto.cs` 的 `InitEc2b` 重新生成；可以把它从 Git 中移除。）

## 待解决的问题

- 暂无。不存在单独的基础设施仓库（维护者决定）；部署只用 `scripts/`。
