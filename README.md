<div align="center">

# March7thHoney

**A playable game server in C# on .NET 10.<br />Clone it, run one command, and log in.**

<p>
  <a href="https://github.com/Mar7thDev/March7thHoney/actions/workflows/ci.yml"><img src="https://github.com/Mar7thDev/March7thHoney/actions/workflows/ci.yml/badge.svg?branch=main" alt="CI" /></a>
  <a href="https://github.com/Mar7thDev/March7thHoney/stargazers"><img src="https://img.shields.io/github/stars/Mar7thDev/March7thHoney?style=flat&logo=github&color=f5c542" alt="Stars" /></a>
  <a href="https://github.com/Mar7thDev/March7thHoney/commits/main"><img src="https://img.shields.io/github/last-commit/Mar7thDev/March7thHoney?color=2ea44f" alt="Last commit" /></a>
  <img src="https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white" alt=".NET 10" />
  <img src="https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%7C%20macOS-0a7ea4" alt="Platform" />
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-AGPL--3.0-blue" alt="License" /></a>
  <a href="https://discord.gg/CyreneEchoes"><img src="https://img.shields.io/badge/Discord-Join-5865F2?logo=discord&logoColor=white" alt="Discord" /></a>
</p>

[Quick start](#quick-start) · [Features](#features) · [Documentation](docs/README.md) · [Contributing](CONTRIBUTING.md) · [Discord](https://discord.gg/CyreneEchoes)

English | [简体中文](docs/project-overview/README.zh-CN.md) | [繁體中文](docs/project-overview/README.zh-TW.md) | [日本語](docs/project-overview/README.ja-JP.md)

</div>

| | |
|---|---|
| **Playable end to end** | Shops, warps, battles, the open world and the endgame challenge modes work against a real client; missions are being filled in. |
| **Built to be run** | SQLite or MySQL, a web admin panel, the MUIP API and a systemd-ready Linux setup. Windows, Linux and macOS on `x64` and `arm64`. |
| **No reflection at run time** | Packet handlers, commands and database stores are registered by Roslyn source generators, so the server also publishes with NativeAOT. |
| **Checked on every change** | A warnings-as-errors build and the regression suites for accounts, the web server, KCP and the source generators run in CI; more suites cover login, scenes and game modes against real resources. |

## Features

| Module | Status | Notes |
|---|---|---|
| Shop system | Stable | Browse and purchase flow works |
| Team formation | Stable | Lineup and slot switching available |
| Warp/Gacha | Stable | Full pull flow and result display |
| Battle flow | Stable | Core combat loop is playable |
| Open-world scene | Stable | Scene loading, interaction, exploration |
| Character progression | Stable | Leveling and promotion loops available |
| Quest system | In progress | Main/common progression works, edge cases remain |
| Friend system | Stable | Core interaction and display |
| Anomaly Arbitration | Stable | Entry/challenge/reward supported |
| Challenge modes | Stable | FH / PF / AS playable |
| Currency War | In progress | Basic gameplay flow available |
| Achievement system | Stable | Most achievements supported |

The full list is in the [gameplay implementation tree](docs/development/gameplay-implementation.md).

## Quick start

You need the .NET SDK 10 (10.0.100 or later) and Git, on Windows, Linux or macOS (`x64` or `arm64`).

```bash
git clone https://github.com/Mar7thDev/March7thHoney.git
cd March7thHoney
dotnet run --project Program/Program.csproj
```

The repository does not contain game resources. The server reads them from a local resource cache; see
[rebuilding the resource cache from a client export](docs/operations/resource-cache-from-client-export.md).

For a Linux server, including a systemd service and updates, follow the
[native Linux guide](docs/getting-started/native-linux.md).

## Build

```bash
dotnet build Program/Program.csproj -c Release
```

The build generates the protocol C# from the checked-in schema, `Proto/StarRail.proto`, using pinned
`Google.Protobuf.Tools` 3.33.1. Generated files live under `Proto/obj/` and are not tracked. Each game version
keeps its protocol on its own version branch. See the [protocol guide](docs/development/protobuf.md) for
updating the schema and for platform requirements.

CI publishes self-contained builds of `main` as workflow artifacts:

| Runtime | Artifact name |
|---|---|
| `win-x64` | `March7thHoney-win-x64` |
| `linux-x64` | `March7thHoney-linux-x64` |
| `linux-arm64` | `March7thHoney-linux-arm64` |

## Documentation

The [documentation index](docs/README.md) lists everything. Starting points:

- Running a server: [native Linux guide](docs/getting-started/native-linux.md), [account security](docs/operations/account-security.md)
- APIs: [MUIP API](docs/api/muip-api.md), [web account API](docs/api/web-account-api.md)
- Working on the code: [contributing guide](CONTRIBUTING.md), [protocol](docs/development/protobuf.md), [source generators](docs/development/source-generators.md)
- Regression suites: [security gate](docs/testing/security-regression.md) and the other documents under `docs/testing/`

## Contributing

Changes are welcome. Read the [contributing guide](CONTRIBUTING.md) for the build, the regression suites to run
and the conventions for commits and pull requests. Report vulnerabilities privately as described in the
[security policy](SECURITY.md).

## Roadmap

1. Improve Currency War and Divergent Universe.
2. Fill remaining quests and special trigger logic.
3. Improve settlement/sync stability in edge cases.
4. Expand event-mode gameplay coverage.
5. Continue fixing UI and state-sync issues.
6. Add more configurable server options.

## Troubleshooting

- Check the server logs under your configured log path.
- `/scene reload` solves most scene problems.
- If behavior looks inconsistent after updating, clear the runtime cache and data and retest with a fresh account.
- Community support: <https://discord.gg/CyreneEchoes>

## Credits

### Collaborators

- [Oriha](https://github.com/OrihaWeave)
- Cyrup
- [Melioli](https://github.com/Melioli)

### Contributors

- [Yuki](https://github.com/Yuki8859)
- [Cyt](https://github.com/qinfyy)
- [Glzd001](https://github.com/Glzd001)
- [blamefire159-debug](https://github.com/blamefire159-debug)
- [AzureXuanVerse](https://github.com/AzureXuanVerse)
- [AcheronWife](https://github.com/AcheronWife)
- [Yuuki](https://github.com/akbaryahya)
- [ShuaiyuMiao](https://github.com/ShuaiyuMiao)

### Projects

- [LunarCore](https://github.com/Melledy/LunarCore)
- [DanhengServer](https://github.com/OrihaWeave/DanhengServer-OpenSource)
- [KcpSharp](https://github.com/yigolden-oss/KcpSharp)

<a href="https://github.com/Mar7thDev/March7thHoney/graphs/contributors"><img src="https://contrib.rocks/image?repo=Mar7thDev/March7thHoney" alt="Contributors" /></a>

## Star history

<a href="https://www.star-history.com/#Mar7thDev/March7thHoney&Date"><img src="https://api.star-history.com/svg?repos=Mar7thDev/March7thHoney&type=Date" alt="Star History Chart" width="100%" /></a>

## License

March7thHoney is licensed under the [GNU Affero General Public License v3.0](LICENSE).
