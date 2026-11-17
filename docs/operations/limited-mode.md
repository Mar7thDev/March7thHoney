# Limited-mode builds

English | [简体中文](limited-mode.zh-CN.md)

A limited-mode build is a build of the server meant to be handed to end users: its configuration is fixed inside
the binary and it has to be activated online before it starts. This page is for whoever builds and distributes
such builds. A normal build from source is not a limited-mode build and never contacts an activation service.

## What limited mode changes

- `Config.json` is neither read nor written. The settings come from `LimitedConfigProvider`
  (`Common/Util/License/LimitedConfigProvider.cs`): the defaults, with a fixed MUIP admin key, accounts created
  automatically on first login, new players starting at Trailblaze level 70 and packet logging turned down.
- Settings changed in the admin panel apply in memory only.
- At startup the server must pass activation, described below. If activation fails the server stops.

## Building

```sh
dotnet publish Program/Program.csproj -c Release -p:LimitedMode=true \
  -p:ActivationServerUrl=https://activation.example.com \
  -p:ActivationPublicKey=<base64 public key> \
  -p:ActivationHelpUrl=example.com/help
```

| Property | Meaning | Default |
| --- | --- | --- |
| `LimitedMode` | `true` defines the `LIMITED_MODE` compilation symbol, which turns limited mode on | not set |
| `ActivationServerUrl` | Base URL of the activation service | this project's service |
| `ActivationPublicKey` | ECDSA P-256 public key the service signs with, as base64 SubjectPublicKeyInfo DER | this project's key |
| `ActivationHelpUrl` | Where the operator is sent when activation fails | this project's community link |

The three activation values are compiled into `March7thHoney.Common` and cannot be changed at run time: a key
the operator could replace would make the signature check meaningless. The defaults are in `Common/Common.csproj`
and belong to this project's own service. Anyone who distributes limited-mode builds of a fork sets their own
service URL and key.

The publish output already holds the configuration defaults from `Config/`, among them the login RSA key pair.
The packages on this repository's Releases page are not limited-mode builds.

## What the activation service has to provide

All requests are JSON `POST`s under `ActivationServerUrl`.

1. **Device authorization**, on the first start or after a token was rejected. The server sends `device_id`,
   `version`, `os` and `hostname` to `/api/launcher/device/code` and expects `device_code`, `user_code`,
   `verification_uri_complete`, `expires_in` and `interval`. It prints the URL and the code, opens a browser, and
   polls `/api/launcher/device/token` with the `device_code` until the reply contains `token`. The errors
   `authorization_pending` and `slow_down` mean keep polling; `access_denied` and `expired_token` end the attempt.
   The token is saved to `Config/activation.token`.
2. **Activation**, on every start. The server sends `hwid`, `nonce` and `version` to `/api/server/activate` with
   the token as a bearer token and expects `hwid`, `nonce`, `exp` and `sig`. It accepts the reply when `hwid` and
   `nonce` are echoed, `exp` (Unix seconds) is in the future, and `sig` is a valid ECDSA P-256 signature with
   SHA-256, in IEEE P1363 format and base64, over these bytes: the ASCII text `M7HACT01`, the 32-byte hardware
   ID, the 24-byte nonce, and `exp` as an 8-byte little-endian integer. A `401` or `403` drops the saved token
   and runs device authorization once more.

Environment variables read by the server:

| Variable | Effect |
| --- | --- |
| `M7H_ACTIVATION_TOKEN` | Token to use instead of `Config/activation.token` |
| `M7H_LAUNCHER_MODE` | Set by a launcher that manages the token itself: the browser flow is never started |
| `M7H_DEVICE_ID` | In launcher mode, the device ID to use instead of the hardware ID |

The gate is a deterrent, not a lock: someone who builds from source can leave it out.
