# Security model

English | [简体中文](SECURITY_MODEL.zh-CN.md)

This is the threat model that security reviews and code scans of March7thHoney start from. To report a vulnerability, follow [SECURITY.md](SECURITY.md).

Built on the repository's own [SECURITY.md](SECURITY.md), [account security guide](docs/operations/account-security.md) and [security regression gate](docs/testing/security-regression.md). No infrastructure-as-code repository was used: deployment in this repository is the systemd/Debian helpers in `scripts/`.

## System overview

A self-hosted .NET 10 private server for a turn-based game client. One process (`Program/Program/EntryPoint.cs`) normally runs both:

- an ASP.NET minimal-API **WebServer** (HTTP, default `0.0.0.0:21000`) serving client dispatch/login, account registration and recovery, the web admin panel, MUIP, the player console and the optional web account API;
- a **GameServer** speaking the game protocol over KCP/UDP (default `0.0.0.0:23301`).

Dispatch and GameServer can also run as separate processes, talking over `/validate_account_token` and `/get_account_info`, authenticated by `ServerOption.ServerConfig.ServerExchangeSecret`. Data lives in SQLite or MySQL (`Common/Database/`). Game resources are a local cache (`Config/Resource.bin`) not in Git.

## Components

| Component | Code | Trust boundary |
|---|---|---|
| **Login & Accounts** | `WebServer/Controllers/DispatchRoutes.cs`, `GateServerRoutes.cs`, `SdkShopRoutes.cs`, `LogServerRoutes.cs`, `WebServer/Handler/*Login*`, `RegistrationHandler.cs`, `AccountRecoveryHandler.cs`, `ComboTokenGranterHandler.cs`, `AuthRateLimiter.cs`, `LoginCryptoHelper.cs`, `WebServer/Pages/LoginPlatform` | Internet → server; unauthenticated |
| **Admin & MUIP** | `WebServer/Controllers/AdminRoutes*.cs`, `MuipServerRoutes.cs`, `WebServer/Handler/AdminSessionManager.cs`, `AdminSettingsRegistry.cs`, `AdminLogBuffer.cs`, `WebServer/Pages/Admin` | Admin key holder / admin roles |
| **Player Console** | `WebServer/Controllers/ConsoleRoutes.cs`, `PlayerConsoleSessionManager.cs`, `WebServer/Pages/PlayerConsole` | Logged-in player → own account only |
| **Server Exchange & Web Account API** | `WebServer/Controllers/ServerExchangeRoutes.cs`, `WebAccountRoutes.cs`, `JsonRoutes.cs`, `HandbookRoutes.cs` | Shared secret / API key between services |
| **GameServer** | `GameServer/`, `KcpSharp/`, `Proto/` | Authenticated game client over UDP; pre-auth handshake |
| **Commands** | `Command/Command/`, `GameServer/Command/` | Player/console/MUIP sender → permission checks (`CommandPermissions.TargetOthers`, per-command `Permission`) |
| **Persistence & Config** | `Common/Database/`, `Common/Configuration/`, `Config/` | Operator-owned |
| **Deployment** | `scripts/*.sh`, `scripts/update-server.sh`, `.github/workflows/ci.yml` | Operator host / CI |

## Entry points and who can reach them

- HTTP dispatch/login/registration/password-forgot/email-verify: anyone who can reach the HTTP port (typically the internet for public servers).
- `/admin/api/*`: admin Bearer session or `X-Admin-Session`, obtained with the admin key (`/admin/api/login`); role-based permissions in `AdminRoutes.Access.cs`.
- `/muip/*`: session from `/muip/create_session` + `/muip/auth_admin` with the admin key, then `exec_cmd` runs server commands against a target UID.
- `/player/*`, `/console/*`: player-console session after account login; `exec` runs commands as that player.
- Web account API (`WebAccountRoutes.cs`): disabled by default (`ServerOption.Auth.WebAccountApi`), key-guarded when enabled.
- `/validate_account_token`, `/get_account_info`: shared `ServerExchangeSecret`; 401 when unset.
- `/sr-tools`, `/sr-tools-export`: JSON tooling routes (`JsonRoutes.cs`) — password checked per UID; an account without a password is reachable by UID alone.
- KCP/UDP game port: anyone; handshake admission limits (`KcpAdmissionRegression`) and source-endpoint binding (`KcpSecurityRegression`); then token-authenticated player session with ~70 request handler groups in `GameServer/Server/Packet/Recv/`.

## Assets

- Admin key and admin sessions (full server control: commands, accounts, settings, logs, admin-key rotation).
- Player accounts: passwords, login/combo tokens, email addresses, recent IPs/device IDs (PII).
- Player game state (inventory, currency, progress) — integrity matters for fairness on shared servers.
- Host integrity: file system (log download `AdminRoutes.Logs.cs`, resource reload), process availability.
- `ServerExchangeSecret`, SMTP credentials (`AccountEmailSender.cs`), database credentials in `Config.json`.

## Attacker profiles

- **Unauthenticated network client** (HTTP or UDP): registration abuse, login bypass, token forgery, rate-limit bypass, DoS of KCP handshake, injection into another player's UDP session.
- **Registered player** (with a game client or the player console): acting on other UIDs (IDOR), command permission escalation, duplicating rewards/currency via repeated or concurrent requests, crashing the server with malformed packets.
- **Holder of a low-privilege admin role**: escalating to full admin.
- **Malicious web page** targeting a logged-in admin's browser: CSRF/XSS against the admin panel and player console.

**Maintainer decision:** who can get an account varies by operator; assume public registration — anyone on the internet can become a registered player. All attacker profiles above are in scope.

## Asset priority

**Maintainer decision:** all assets above matter equally; no asset is ranked above the others.

## Existing controls

- Auth rate limiting (`AuthRateLimiter.cs`), device/IP block rules, account bans, token revocation (see account security guide).
- Trusted-proxy allow-list for `X-Forwarded-*` (`HttpServer.TrustedProxies`, default loopback only).
- Login RSA (`Config/Security/LoginRsa*.pem`) for password transport.
- KCP: conversation ID used only for routing; endpoint bound at creation; handshake capacity limits.
- Command permission checks in `CommandManager.cs`.
- Regression suites run in CI: `scripts/verify-security.py`, Auth/WebServer/Kcp/SourceGen regressions.

## Out of scope / accepted risk (from SECURITY.md)

- The game client and its publisher's services.
- Problems that need access the operator already has: the server's own `Config.json`, database or host.
- The UDP endpoint check is not cryptographic; source-address spoofing is accepted, mitigated by firewalls (security-regression.md).
- `System.Security.Cryptography.Pkcs` advisory: pinned to fixed version, no known reachable entry point.
- Device IDs are client-supplied and not unforgeable (account-security.md).
- **Maintainer decision:** the committed login RSA key pair (`Config/Security/LoginRsaPrivateKey.pem`, `LoginRsaPublicKey.pem`, public test material shared with CyreneLauncher per README) and `Config/ClientSecretKey.ec2b` are intentional and public: accepted risk. Findings that only report these committed keys, or confidentiality that depends on them, are accepted risk. (`ClientSecretKey.ec2b` is regenerated by `Common/Util/Crypto.cs` `InitEc2b` when missing; removing it from Git is acceptable.)

## Open questions

- None outstanding. No separate infrastructure repository exists (maintainer decision); deployment is `scripts/` only.
