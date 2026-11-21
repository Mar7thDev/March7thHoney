# Web 后台回归

[English](web-server-regression.md) | 简体中文

`WebServerRegression` 检查 Web 后台、MUIP、命令目标隔离和异步命令输出。修改 `WebServer/`、后台或玩家控制台页面、MUIP 或命令到达目标的方式后运行。

项目：[`scripts/WebServerRegression/`](../../scripts/WebServerRegression/)

## 运行方式

从仓库根目录执行：

```powershell
dotnet run --project scripts/WebServerRegression/WebServerRegression.csproj
```

需要在浏览器中检查页面时，追加 `--serve`：

```powershell
dotnet run --project scripts/WebServerRegression/WebServerRegression.csproj -- --serve
```

打开 `http://127.0.0.1:21000/admin/index.html`，使用测试密钥 `local-web-regression-only` 登录，完成后按 Ctrl+C 停止。此模式只在回环地址上启动测试后台，不要用它验证配置或存库操作。

## 前提条件

只需要 .NET SDK。回归使用独立的内存数据和临时目录，不加载项目的 `Config.json` 或任何玩家数据库。模拟连接和命令只在测试进程内注册。

## 覆盖范围

### 命令与后台

- 无目标命令、目标隔离、异步输出和异常、并发请求。
- `/language` 对后台控制台回复仅限玩家使用；玩家自助控制台可以固定自身语言，并以新语言回复。
- User 角色玩家执行 `/json export` 时拒绝路径穿越和绝对路径名称，由玩家名生成的默认文件名也留在 `Config/Json` 内；`/json <路径>` 拒绝导入 `Config/Json` 之外的文件。
- 登录保护及维护接口的内存数据。

### MUIP 与网关

- MUIP GET 与 POST；命令处理器只能注册一个。
- MUIP 服务器信息列出在线玩家；玩家信息把尚未完成登录初始化的在线玩家报告为探索，把离线玩家报告为离线。
- `query_gateway` 下发差分宇宙 BuildRef 地址与请求头；两个精确的 BuildRef JSON 路由。

### 凭据撤销与 MUIP 会话生命周期

- 无论 RSA 登录解密开启还是关闭，`/sr-tools` 都拒绝空密码或错误密码；正确密码仍进入正常的在线状态检查。
- 玩家控制台令牌绑定签发时的密码版本。管理员改密或邮件找回密码之后，旧令牌不能继续使用，新登录仍可用。此机制不需要新增数据库字段。
- MUIP 会话绑定创建时的管理密钥版本。配置变更后，认证、命令、服务器信息和玩家信息接口都拒绝旧会话。在后台轮换密钥也会清除 MUIP 管理会话。
- 匿名 MUIP 会话达到容量上限时拒绝新建。已过期且被遗弃的会话在新建时清理，服务也每分钟主动清理一次。总上限为 1024，每个 HTTP 来源最多 32 个尚未过期的会话。后台页面和玩家控制台为每条命令单独建立会话，该会话不进入 MUIP 会话表，因此既不使用创建接口，也不占用其容量。
- `/muip/auth_admin` 收到错误密钥时删除该 MUIP 会话，并计入与后台登录相同的按客户端失败计数。同一客户端错误 8 次后，即使密钥正确也会被拒绝（code 3），MUIP 与后台登录都如此，其他客户端不受影响。
- 根 `/sr-tools` 导入不再允许仅凭 UID 操作，即使关闭了 RSA 解密；没有密码的旧账号需要先设置密码。
- SDK `createOrder` 只给出示了自身 combo 或 dispatch 令牌（通过 `x-rpc-combo_token` 或请求参数）的在线玩家入账：缺少令牌、伪造令牌、使用其他账号的令牌或完全不带 UID 时，玩家的月卡状态保持不变；不再回退到唯一在线玩家。
- 无密码账号会被控制台登录、会话、命令目录、命令执行与自助重置拒绝，也会被 `/sr-tools` 与 `/sr-tools-export` 拒绝。
- `/sr-tools-export` 拒绝缺失或错误的密码，接受 `POST` 请求体中的凭据，对旧的查询字符串 `GET` 返回 405。
- 控制台、`/sr-tools` 或 `/sr-tools-export` 上的错误密码触发登录限流后，三者都会拒绝正确密码。两个 sr-tools 路由同样执行 IP 封锁与账号封禁。
- `DefaultPermissions` 默认为空；默认角色账号没有 `*`、`command.*`、`command.account` 或 `command.others`；启动警告会标出 `*` 与高权限条目，但不会标出 User 角色已有的权限。测试夹具不再自行清空 `DefaultPermissions`，因此控制台权限检查在默认配置下运行。

### 私聊

- 私聊消息只在互为好友的玩家之间投递和保存：发给非好友、发给自己、发给自己拉黑的玩家，或被对方拉黑的发送者发出的消息都会被拒绝，双方都不保存。
- 一次请求的接收者超过 `FriendManager.MaxMessageTargets` 时整体拒绝，不投递任何消息；混合请求仍投递给好友，并返回第一个拒绝原因。
- 每段保存的对话（包括离线好友的）最多保留 `FriendManager.MaxConversationHistory` 条消息。

这些用例只使用临时账号、临时数据库和回环 HTTP，不接触真实玩家存档。

## 预期结果

运行结束时输出：

```text
Regression failures: 0
```

回归会打印维护接口的耗时。异常测试会打印一条预期的错误日志。
