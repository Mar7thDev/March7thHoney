# 安全修复回归入口

从仓库根目录执行（.NET SDK 10、Python 3）：

```sh
python3 scripts/verify-security.py
```

该入口执行完整 Release 构建、Auth/Web 鉴权与会话安全回归、KCP 来源绑定与握手容量回归、协议生成回归，并校验当前版本两个 `CmdIds.cs` 副本一致。测试只使用临时数据库、内存资源和回归程序自己的隔离配置；不部署生产服务。

默认明确跳过依赖真实游戏资源的 LoginScene、GridFight、DailyActive 回归。拥有匹配版本的 `Config/Resource.bin` 时可使用 `--with-resources`；指定后文件缺失会失败，不会把跳过报告为通过。客户端实机、Windows/macOS、NativeAOT 和真实业务数据升级仍须单独验收。

Linux 更新脚本的失败/回滚场景使用隔离 mock 命令测试，绝不操作真实 systemd 服务。见运维文档和对应测试脚本。

## 依赖补丁

WebServer 显式固定 `System.Security.Cryptography.Pkcs` 10.0.13，覆盖 MimeKit 传递依赖的旧最低版本。Microsoft 公告列出的修复线为 10.0.13：

- https://github.com/dotnet/runtime/security/advisories/GHSA-2fmv-rv2c-59x7
- https://github.com/dotnet/runtime/security/advisories/GHSA-846f-f764-76mx

项目没有已确认可达的攻击者 CMS 处理入口；补丁消除已知依赖风险，不把它描述为已验证的本项目远程漏洞。

## UDP 会话边界

KCP conversation ID 仅用于路由，不再允许其它来源 IP／端口向既有会话注入数据。两种通道均在解析和入队前核对创建时的 endpoint；测试只向内存接收队列注入合成数据，不发出网络流量。客户端 IP／端口变化须重新握手，这与通道固定发送目的地址一致。该检查不是加密身份认证，不能抵御能伪造同一来源地址的网络攻击；公网部署仍需防火墙／上游流量限制。
