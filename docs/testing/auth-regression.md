# 账号安全回归

测试项目：[`scripts/AuthRegression/`](../../scripts/AuthRegression/)

```powershell
dotnet run --project scripts/AuthRegression/AuthRegression.csproj
```

使用全新的临时 SQLite 数据库和测试配置，不读取项目 `Config.json` 或实际玩家数据库。覆盖旧表迁移、注册与登录绕过、并发注册、管理员鉴权、IP 归一化与转发头、独立封锁、令牌撤销、批量删除、事务回滚、断开会话、延迟写入、存库与删除并发、重载后的封锁和删除记录，分离部署的玩家 IP 检查，以及网站游戏账号接口（开关与密钥、代理转发后的来源地址、不区分大小写的用户名唯一、并发创建、改密吊销令牌、封禁/解封、关闭注册后的外部注册跳转）。

预期最终输出 `Auth regression failures: 0`。模拟游戏会话没有真实 KCP 传输，断开测试会产生预期的发送失败日志。

封禁回归还覆盖：账号封禁仅影响自身 UID；共享 PC 设备或公网 IP 的其他账号仍可通过密码登录、令牌验证、Combo 换票与注册；被封禁 UID 更换设备仍被拒绝；独立设备 / IP 规则继续拦截，包括省略设备 ID 的历史设备检查；公共占位指纹及旧哈希被过滤；拒绝日志能区分账号封禁与独立规则，且不包含密码、令牌或原始设备值。这些检查使用合成账号与临时数据库，不替代 PC / Android 客户端实机验证。

```powershell
dotnet run --project scripts/AuthRegression/AuthRegression.csproj -- --serve
```

先执行回归，通过后在 `http://127.0.0.1:21001/admin/index.html` 启动隔离的后台测试夹具，使用测试密钥 `local-auth-regression-only` 登录。夹具带有超过一页的合成账号与 IP/设备记录，所有记录均可丢弃。完成后 Ctrl+C 停止。不要将此测试实例暴露到外网。
