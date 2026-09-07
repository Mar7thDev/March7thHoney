运行后台回归检查：

```powershell
dotnet run --project scripts/WebServerRegression/WebServerRegression.csproj
```

检查无目标命令、目标隔离、异步输出和异常、并发请求、MUIP GET/POST、登录保护及维护接口内存数据；同时打印维护接口耗时。异常测试会打印一条预期的错误日志，最终以 `Regression failures: 0` 为通过。

测试使用独立内存数据和临时目录，不加载项目的 `Config.json` 或玩家数据库。模拟连接和命令仅在测试进程内注册。

需要验证浏览器页面时：

```powershell
dotnet run --project scripts/WebServerRegression/WebServerRegression.csproj -- --serve
```

打开 `http://127.0.0.1:21000/admin/index.html`，使用测试密钥 `local-web-regression-only` 登录，完成后按 Ctrl+C 停止。此模式只启动回环地址上的测试后台；配置及存库操作不用于验证。
