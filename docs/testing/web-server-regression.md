# Web admin regression

English | [简体中文](web-server-regression.zh-CN.md)

`WebServerRegression` checks the web admin, MUIP, command target isolation and asynchronous command output. Run it after changing `WebServer/`, the admin or player console pages, MUIP or how commands reach a target.

Project: [`scripts/WebServerRegression/`](../../scripts/WebServerRegression/)

## How to run

From the repository root:

```powershell
dotnet run --project scripts/WebServerRegression/WebServerRegression.csproj
```

To check the pages in a browser, add `--serve`:

```powershell
dotnet run --project scripts/WebServerRegression/WebServerRegression.csproj -- --serve
```

Open `http://127.0.0.1:21000/admin/index.html` and sign in with the test key `local-web-regression-only`. Stop it with Ctrl+C when you are done. This mode only starts a test admin on the loopback address; do not use it to verify configuration or database writes.

## Prerequisites

None beyond the .NET SDK. The regression uses its own in-memory data and a temporary directory, and does not load the project's `Config.json` or any player database. Simulated connections and commands are registered only inside the test process.

## What it covers

### Commands and admin

- Commands without a target, target isolation, asynchronous output and exceptions, concurrent requests.
- `/language` answers the admin console that it is player-only; the player self-service console can pin its own language and replies in the new language.
- Login protection and the in-memory data of the maintenance endpoints.

### MUIP and gateway

- MUIP GET and POST.
- `query_gateway` hands out the Divergent Universe BuildRef address and request headers; the two exact BuildRef JSON routes.

### Credential revocation and MUIP session lifecycle

- `/sr-tools` rejects an empty or wrong password whether RSA login decryption is on or off; a correct password still goes on to the normal online-status check.
- Player console tokens are bound to the password version at issue time. After an admin password change or an e-mail password reset the old token stops working, and a new login still works. This needs no new database column.
- MUIP sessions are bound to the admin key version at creation time. After a configuration change, the authentication, command, server-info and player-info endpoints all reject old sessions. Rotating the key from the admin page also clears MUIP admin sessions.
- Anonymous MUIP session creation is refused at capacity. Expired, abandoned sessions are cleaned up on creation, and the service also cleans them up every minute. The total limit is 1024, with at most 32 unexpired sessions per HTTP source. The internal short-lived command bridge does not use the anonymous creation endpoint.
- Root `/sr-tools` import no longer accepts a UID alone, even with RSA decryption off; old accounts without a password must set one first.
- SDK `createOrder` only credits the online player whose combo or dispatch token is presented (in `x-rpc-combo_token` or the request): a missing, forged or other account's token, or no uid at all, leaves the player's supply pass untouched; there is no fallback to the only online player.

These cases use only temporary accounts, a temporary database and loopback HTTP, and never touch real player saves.

## Expected result

The run ends with:

```text
Regression failures: 0
```

The regression prints how long the maintenance endpoints take. The exception test prints one expected error log.
