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
- `/json export` from a User-role player refuses traversal and absolute names and keeps a default name built from the player name inside `Config/Json`; `/json <path>` refuses to import a file outside `Config/Json`.
- Login protection and the in-memory data of the maintenance endpoints.

### MUIP and gateway

- MUIP GET and POST.
- `query_gateway` hands out the Divergent Universe BuildRef address and request headers; the two exact BuildRef JSON routes.

### Credential revocation and MUIP session lifecycle

- `/sr-tools` rejects an empty or wrong password whether RSA login decryption is on or off; a correct password still goes on to the normal online-status check.
- Player console tokens are bound to the password version at issue time. After an admin password change or an e-mail password reset the old token stops working, and a new login still works. This needs no new database column.
- MUIP sessions are bound to the admin key version at creation time. After a configuration change, the authentication, command, server-info and player-info endpoints all reject old sessions. Rotating the key from the admin page also clears MUIP admin sessions.
- Anonymous MUIP session creation is refused at capacity. Expired, abandoned sessions are cleaned up on creation, and the service also cleans them up every minute. The total limit is 1024, with at most 32 unexpired sessions per HTTP source. The internal short-lived command bridge does not use the anonymous creation endpoint.
- A wrong key at `/muip/auth_admin` deletes that MUIP session and counts against the same per-client failure counter as the admin page login. After 8 wrong keys from one client the correct key is refused (code 3) for that client, on both MUIP and the admin page, while other clients are unaffected.
- Root `/sr-tools` import no longer accepts a UID alone, even with RSA decryption off; old accounts without a password must set one first.
- SDK `createOrder` only credits the online player whose combo or dispatch token is presented (in `x-rpc-combo_token` or the request): a missing, forged or other account's token, or no uid at all, leaves the player's supply pass untouched; there is no fallback to the only online player.
- A passwordless account is refused by console login, session, catalog, command execution and self-reset, and by `/sr-tools` and `/sr-tools-export`.
- `/sr-tools-export` rejects a missing or wrong password, accepts credentials in the `POST` body, and answers the old query-string `GET` with 405.
- Once wrong passwords on the console, `/sr-tools` or `/sr-tools-export` trigger the login rate limit, the correct password is refused on all three. An IP block and an account ban are also enforced on both sr-tools routes.
- `DefaultPermissions` defaults to empty, a default-role account has no `*`, `command.*`, `command.account` or `command.others`, and the startup warning flags `*` and high-privilege entries but not User-role permissions. The fixture no longer clears `DefaultPermissions` itself, so the console permission checks run under the default configuration.

### Private chat

- A private message is delivered and stored only between mutual friends: messages to a non-friend, to oneself, to a player one has blocked, or from a sender the target has blocked are rejected and stored on neither side.
- A request naming more than `FriendManager.MaxMessageTargets` recipients is rejected without delivering anything; a mixed request still delivers to friends and reports the first rejection.
- Each stored conversation, including an offline friend's, keeps at most `FriendManager.MaxConversationHistory` messages.

These cases use only temporary accounts, a temporary database and loopback HTTP, and never touch real player saves.

## Expected result

The run ends with:

```text
Regression failures: 0
```

The regression prints how long the maintenance endpoints take. The exception test prints one expected error log.
