# 受限模式构建

[English](limited-mode.md) | 简体中文

受限模式构建是面向最终用户分发的服务端构建：配置固定在程序内部，启动前必须在线激活。本页面向构建和分发这类构建的人。普通的源码构建不是受限模式构建，永远不会连接激活服务。

## 受限模式改变了什么

- 不读取也不写入 `Config.json`。设置来自 `LimitedConfigProvider`（`Common/Util/License/LimitedConfigProvider.cs`）：在默认值的基础上，MUIP 管理密钥固定、首次登录时自动创建账号、新玩家从开拓等级 70 开始，并降低了数据包日志的输出。
- 在后台面板里修改的设置只在内存中生效。
- 启动时服务端必须通过下文所述的激活。激活失败则服务端停止。

## 构建

```sh
dotnet publish Program/Program.csproj -c Release -p:LimitedMode=true \
  -p:ActivationServerUrl=https://activation.example.com \
  -p:ActivationPublicKey=<base64 公钥> \
  -p:ActivationHelpUrl=example.com/help
```

| 属性 | 含义 | 默认值 |
| --- | --- | --- |
| `LimitedMode` | 为 `true` 时定义编译符号 `LIMITED_MODE`，从而开启受限模式 | 未设置 |
| `ActivationServerUrl` | 激活服务的基础地址 | 本项目的服务 |
| `ActivationPublicKey` | 激活服务签名所用的 ECDSA P-256 公钥，base64 编码的 SubjectPublicKeyInfo DER | 本项目的公钥 |
| `ActivationHelpUrl` | 激活失败时提示运营者前往的地址 | 本项目的社区链接 |

这三个激活参数会编译进 `March7thHoney.Common`，运行时无法修改：如果运营者可以替换公钥，签名校验就失去了意义。默认值写在 `Common/Common.csproj` 里，属于本项目自己的服务。分发分叉版本的受限模式构建时，需要设置自己的服务地址和公钥。

publish 输出已经带有 `Config/` 中的默认配置，其中包括登录 RSA 密钥对。Releases 页面上供 CyreneLauncher 下载的 `win-x64.zip` 就是这样一个构建，发布时加上 `-r win-x64 -p:DoAot=true`，再补入 `Config/Resource.bin` 并去掉调试符号。启动器把它解压覆盖到自己的服务端目录，因此更新会替换随包的文件，并保留 `Config/Database/` 和 `Config/activation.token`。

## 激活服务需要提供什么

所有请求都是发往 `ActivationServerUrl` 下的 JSON `POST`。

1. **设备授权**，在首次启动或令牌被拒绝之后进行。服务端向 `/api/launcher/device/code` 发送 `device_id`、`version`、`os` 和 `hostname`，期望得到 `device_code`、`user_code`、`verification_uri_complete`、`expires_in` 和 `interval`。它会打印地址和授权码、打开浏览器，然后带着 `device_code` 轮询 `/api/launcher/device/token`，直到响应里出现 `token`。错误 `authorization_pending` 和 `slow_down` 表示继续轮询；`access_denied` 和 `expired_token` 表示本次尝试结束。令牌保存到 `Config/activation.token`。
2. **激活**，每次启动都会进行。服务端以该令牌作为 bearer 令牌，向 `/api/server/activate` 发送 `hwid`、`nonce` 和 `version`，期望得到 `hwid`、`nonce`、`exp` 和 `sig`。满足以下条件时才接受响应：`hwid` 和 `nonce` 原样返回，`exp`（Unix 秒）在未来，并且 `sig` 是对下列字节的有效签名（ECDSA P-256 配 SHA-256，IEEE P1363 格式，base64 编码）：ASCII 文本 `M7HACT01`、32 字节的硬件 ID、24 字节的 nonce，以及以 8 字节小端整数表示的 `exp`。收到 `401` 或 `403` 时会丢弃已保存的令牌，并再做一次设备授权。

服务端读取的环境变量：

| 变量 | 作用 |
| --- | --- |
| `M7H_ACTIVATION_TOKEN` | 使用这个令牌，而不是 `Config/activation.token` |
| `M7H_LAUNCHER_MODE` | 由自行管理令牌的启动器设置：不会启动浏览器流程 |
| `M7H_DEVICE_ID` | 启动器模式下，用这个设备 ID 代替硬件 ID |

这道关卡只起威慑作用，并不是锁：从源码构建的人可以不带它。
