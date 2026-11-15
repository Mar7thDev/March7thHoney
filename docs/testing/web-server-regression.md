# Web 后台回归

测试项目：[`scripts/WebServerRegression/`](../../scripts/WebServerRegression/)

运行后台回归检查：

```powershell
dotnet run --project scripts/WebServerRegression/WebServerRegression.csproj
```

检查无目标命令、目标隔离、异步输出和异常、并发请求、MUIP GET/POST、登录保护及维护接口内存数据；同时验证 `query_gateway` 下发差分宇宙 BuildRef 地址与请求头，以及两个精确 BuildRef JSON 路由；`/language` 对后台控制台返回仅限玩家，玩家自助控制台可固定自身语言并以新语言回复。测试会打印维护接口耗时；异常测试会打印一条预期的错误日志，最终以 `Regression failures: 0` 为通过。

测试使用独立内存数据和临时目录，不加载项目的 `Config.json` 或玩家数据库。模拟连接和命令仅在测试进程内注册。

需要验证浏览器页面时：

```powershell
dotnet run --project scripts/WebServerRegression/WebServerRegression.csproj -- --serve
```

打开 `http://127.0.0.1:21000/admin/index.html`，使用测试密钥 `local-web-regression-only` 登录，完成后按 Ctrl+C 停止。此模式只启动回环地址上的测试后台；配置及存库操作不用于验证。

## 凭据撤销与 MUIP 生命周期

安全回归还检查：

- `/sr-tools` 在 RSA 登录解密开启或关闭时，都拒绝空密码／错误密码；正确密码仍进入正常在线状态检查。
- 玩家控制台令牌绑定签发时的密码版本；管理改密及邮件找回密码之后，旧令牌不能继续使用，新登录仍可用。此机制不依赖数据库新增字段。
- MUIP 会话绑定创建时的管理密钥版本；配置变更后，认证、命令、服务器信息和玩家信息接口均拒绝旧会话。后台轮换密钥也会主动清除 MUIP 管理会话。
- 匿名 MUIP 会话达到容量上限时拒绝新增；已过期且被遗弃的会话会在新建时清理，服务也每分钟主动清理一次。总上限 1024，每个 HTTP 来源最多 32 个尚未过期会话；内部短期命令桥接不使用匿名创建接口。

这些用例仅使用临时账号、临时数据库和回环 HTTP，不接触真实玩家存档。根 `/sr-tools` 导入不再允许仅凭 UID 操作，即使关闭 RSA 解密；没有密码的旧账号需要先设置密码。
