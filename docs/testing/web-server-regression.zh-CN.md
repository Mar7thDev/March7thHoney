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
- 登录保护及维护接口的内存数据。

### MUIP 与网关

- MUIP GET 与 POST。
- `query_gateway` 下发差分宇宙 BuildRef 地址与请求头；两个精确的 BuildRef JSON 路由。

### 凭据撤销与 MUIP 会话生命周期

- 无论 RSA 登录解密开启还是关闭，`/sr-tools` 都拒绝空密码或错误密码；正确密码仍进入正常的在线状态检查。
- 玩家控制台令牌绑定签发时的密码版本。管理员改密或邮件找回密码之后，旧令牌不能继续使用，新登录仍可用。此机制不需要新增数据库字段。
- MUIP 会话绑定创建时的管理密钥版本。配置变更后，认证、命令、服务器信息和玩家信息接口都拒绝旧会话。在后台轮换密钥也会清除 MUIP 管理会话。
- 匿名 MUIP 会话达到容量上限时拒绝新建。已过期且被遗弃的会话在新建时清理，服务也每分钟主动清理一次。总上限为 1024，每个 HTTP 来源最多 32 个尚未过期的会话。内部短期命令桥接不使用匿名创建接口。
- `/muip/auth_admin` 收到错误密钥时删除该 MUIP 会话，并计入与后台登录相同的按客户端失败计数。同一客户端错误 8 次后，即使密钥正确也会被拒绝（code 3），MUIP 与后台登录都如此，其他客户端不受影响。
- 根 `/sr-tools` 导入不再允许仅凭 UID 操作，即使关闭了 RSA 解密；没有密码的旧账号需要先设置密码。

这些用例只使用临时账号、临时数据库和回环 HTTP，不接触真实玩家存档。

## 预期结果

运行结束时输出：

```text
Regression failures: 0
```

回归会打印维护接口的耗时。异常测试会打印一条预期的错误日志。
