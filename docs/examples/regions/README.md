# Regional Server Setup Examples

These examples show a split deployment where one public dispatch server lists your custom regions, and each region runs its own gateway/game server. The dispatch process can also be a playable region when `RunGateway` is enabled.

There are two common layouts:

- Combined single server: one process runs dispatch, gateway, and game server. Use `RunDispatch = true`, `RunGateway = true`, and leave `Regions` empty unless you have extra remote regions.
- Split multi-region: one process runs dispatch/login only, and separate gateway/game nodes run each playable region. Use `RunGateway = false` on the dispatch-only node and put every playable region in `Regions`.

The important bit: the visible name in the server list comes from `GameServerName`, but the region identity the client remembers comes from `GameServerId`. Use your own stable IDs like `hoyotoon_hsr_na`; avoid official IDs such as `prod_official_asia`, `prod_official_usa`, or `prod_official_eur` unless you intentionally want to reuse those client mappings.

The dispatch response also carries a default selected region name. Set `ServerConfig.DefaultRegionName` to one of the advertised `GameServerId` values. If it is blank, the server uses the first advertised region automatically.

## Files

- `Config.dispatch.example.json` runs a dispatch-only HTTP server and advertises remote regions.
- `Config.region-gateway.example.json` runs one selectable region. Copy it once per region and change the ID, display name, database name, HTTP host, and public game address.

The example JSON files are intentionally minimal. Missing settings come from the defaults in `ConfigContainer`, and the app will write the expanded full config back to `Config.json` after startup.

## Network Shape

```text
Client
  HTTPS /query_dispatch and login APIs
    -> reverse proxy
    -> dispatch node HTTP port, for example 21000

Client
  HTTPS /query_gateway
    -> reverse proxy
    -> selected region gateway HTTP port, for example 21001

Client
  UDP/KCP game traffic
    -> router/NAT port forward
    -> selected region game port, for example UDP 23301
```

A normal HTTP reverse proxy can handle dispatch, login, and `query_gateway`. The game connection is not HTTP; forward the configured `GameServer.Port` as UDP/KCP to the game server.

## Dispatch Node

Use `Config.dispatch.example.json` for a dispatch-only server behind your main dispatch domain, for example `https://dispatch.example.com`.

Key fields:

- `HttpServer.PublicAddress`: public dispatch hostname, without `https://`.
- `GameServer.GameServerName`: dispatch/login page branding. It is not added to the region list while `RunGateway` is `false`.
- `GameServer.UsePacketEncryption`: `false` avoids initializing game packet encryption on a dispatch-only node.
- `Database`: still needed because dispatch owns accounts, login tokens, registration, bans, and server exchange validation.
- `ServerOption.Auth`: dispatch/login behavior. Add email SMTP fields here only if verification or password reset email is enabled.
- `ServerOption.ServerConfig.RunDispatch`: `true`.
- `ServerOption.ServerConfig.RunGateway`: `false` for dispatch-only, or `true` if this same process should also host a local game region.
- `ServerOption.ServerConfig.ServerExchangeSecret`: shared secret used by region gateways when validating dispatch-issued tokens.
- `ServerOption.ServerConfig.DefaultRegionName`: selected/recommended region ID returned in dispatch. Use a value that matches one advertised `GameServerId`.
- `ServerOption.ServerConfig.Regions`: extra remote regions returned by `/query_dispatch`.

When `RunGateway` is `true`, the dispatch response automatically includes the local server as a playable region using `GameServer.GameServerId`, `GameServer.GameServerName`, and the local `/query_gateway` URL. `Regions` is only for additional remote regions.

When `RunGateway` is `false`, the dispatch node does not start the game listener and does not advertise its local `GameServer` as a selectable region. In that mode, gameplay-facing fields such as `GameServer.PublicAddress`, `GameServer.Port`, `GameServer.GameServerId`, and `GameServer.EnvType` do not affect the playable regions. Keep the actual region settings in `ServerConfig.Regions` and in each region gateway config.

Each region entry needs:

- `GateWayAddress`: full public HTTPS URL to that region's `/query_gateway` route.
- `GameServerName`: display name shown in the in-game server catalog.
- `GameServerId`: stable custom region key. Keep it unique and do not rename it casually after users have selected it.
- `EnvType`: usually `2` for production-style regions.

Example region entry:

```json
{
  "GateWayAddress": "https://na-gateway.example.com/query_gateway",
  "GameServerName": "HoyoToon Live NA",
  "GameServerId": "hoyotoon_hsr_na",
  "EnvType": 2
}
```

## Region Gateway/Game Node

Use `Config.region-gateway.example.json` on each region host.

Key fields:

- `HttpServer.PublicAddress`: public hostname used by your reverse proxy for this region's HTTP gateway, without `https://`.
- `HttpServer.Port`: local HTTP port the reverse proxy forwards to.
- `HttpServer.SendHotfix`: controls whether `/query_gateway` returns configured hotfix URLs.
- `GameServer.PublicAddress`: public DNS name or IP returned to the client for the actual game connection.
- `GameServer.Port`: UDP/KCP game port to forward to this machine.
- `GameServer.GameServerId`: must match the dispatch `Regions[].GameServerId`.
- `GameServer.GameServerName`: should match the dispatch `Regions[].GameServerName`.
- `ServerOption.ServerConfig.RunDispatch`: `false`.
- `ServerOption.ServerConfig.RunGateway`: `true`.
- `ServerOption.ServerConfig.FromDispatchBaseUrl`: public base URL of the dispatch server.
- `ServerOption.ServerConfig.ServerExchangeSecret`: must match the dispatch config.
- `Database`: stores this region's player/game data. Keep it separate per region.

If you run multiple regions, give each region its own database file or database name.

## Combined Dispatch + Region

For the default one-server setup, the dispatch is also its own region. Use one config file with both services enabled:

```json
{
  "GameServer": {
    "PublicAddress": "game.example.com",
    "Port": 23301,
    "GameServerId": "hoyotoon_hsr_live",
    "GameServerName": "HoyoToon Live"
  },
  "ServerOption": {
    "ServerConfig": {
      "RunDispatch": true,
      "RunGateway": true,
      "DefaultRegionName": "hoyotoon_hsr_live",
      "Regions": []
    }
  }
}
```

In that layout:

- `/query_dispatch` is served by the dispatch HTTP host.
- `/query_gateway` is served by the same process.
- The server catalog shows one row from `GameServerName`.
- The selected region key is `GameServerId`.
- `DefaultRegionName` should match `GameServerId`, or be left blank to use the local region automatically.
- The actual game connection uses `GameServer.PublicAddress:GameServer.Port`.

Add entries to `Regions` only when this dispatch should also advertise other gateway/game nodes.

## Reverse Proxy Notes

- Proxy dispatch HTTP requests to the dispatch node's `HttpServer.Port`.
- Proxy each region's `/query_gateway` URL to that region node's `HttpServer.Port`.
- Preserve `Host`, `X-Forwarded-Proto`, `X-Forwarded-For`, and/or `X-Real-IP` headers when possible.
- If the proxy terminates TLS, keep `HttpServer.UseSSL` as `false` unless the app itself is serving HTTPS.
- Always include `/query_gateway` in `Regions[].GateWayAddress`.
- Port-forward the game server as UDP to `GameServer.Port`; do not send that traffic through the HTTP proxy.

## Setup Checklist

1. Choose combined single server or split multi-region.
2. For combined mode, use one config with `RunDispatch = true`, `RunGateway = true`, and your local `GameServerId`/`GameServerName`.
3. For split mode, copy `Config.dispatch.example.json` to the dispatch server as `Config.json`.
4. Set `HttpServer.PublicAddress` to your dispatch hostname.
5. Set one strong `ServerExchangeSecret`.
6. Add one `Regions` entry per remote playable region.
7. Copy `Config.region-gateway.example.json` to each remote region server as `Config.json`.
8. Set each region's `HttpServer.PublicAddress`, `GameServer.PublicAddress`, `GameServer.Port`, `GameServerId`, and `GameServerName`.
9. Use the same `ServerExchangeSecret` on every region gateway.
10. Set each region's `FromDispatchBaseUrl` to the dispatch public URL.
11. Configure the reverse proxy for dispatch and gateway HTTP.
12. Port-forward each region's UDP game port.

## Troubleshooting

- Only one row appears: check dispatch `ServerConfig.Regions`, and remember that `RunGateway = false` means the dispatch node will not add itself as a playable region.
- The row has the wrong visible name: check `GameServerName` in the dispatch region entry.
- The client still defaults to an official region such as Asia: make sure `DefaultRegionName` matches one advertised `GameServerId`, then restart the client or clear its cached server selection.
- The client seems to remember an official region: change away from official-looking `GameServerId` values and clear the client's cached server selection.
- Region selection works but entering the game fails: verify the region's `/query_gateway` reverse proxy first, then verify UDP port forwarding to `GameServer.PublicAddress:GameServer.Port`.
- Login succeeds but gateway validation fails: make sure `FromDispatchBaseUrl` reaches the dispatch server and `ServerExchangeSecret` matches on both sides.
