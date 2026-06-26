# Proto

Version-aware, CmdType-grouped protobuf definitions for the server.

## Layout

```
Proto/
  <version>/                 active + reference game versions (e.g. 4.4)
    Raw/                     authoritative dump for that version (Mar7thDumper)
      StarRail.proto         full AST: 589 enums + 4469 messages, all Cmd*Type enums,
                             // cmdid / // obf annotations  (closure source)
      packetIds.txt          cmdid -> message
      translations.txt       obf -> real name
      Gateserver.proto, Mar7thDumper.log
      dump.cs                119MB il2cpp dump (git-ignored)
    Common.proto             types referenced by >=2 CmdType groups
    Cmd<Xxx>Type.proto       one file per command category: the enum + its closure
    Orphans.proto            curated types not reachable from any packet (compiled catch-all)
  Generated/                 generated C# for the ACTIVE version (the only thing Proto.csproj compiles)
  Tool/ProtoOrganizer/       the reorganizer (see below)
```

The grouped `Proto/<version>/*.proto` tree is the source of truth — edit definitions there. (The old
flat `ProtoFile/` library has been retired; `Proto/4.4/` is a complete, verified-equivalent replacement.)

Only **one** version is active and compiles into the `March7thHoney.Proto` assembly via
`Proto/Generated/`. Other versions are kept as data/translation reference. C# references proto types by
real name (e.g. `Handler<FinishTutorialCsReq>`) — unchanged by the regrouping.

## Regenerating the active version

Replaces the old `RefreshProtoFromLunarCoreCyt.ps1`. The tool parses the curated definitions, groups
them by Cmd*Type via the master AST + `packetIds.txt`, factors shared types into `Common.proto`, emits
the grouped tree, and runs `protoc` to (re)generate `Proto/Generated/*.cs`.

```sh
# dry run: print the grouping report, write nothing
dotnet run --project Proto/Tool/ProtoOrganizer -- --report

# emit Proto/4.4 + regenerate Proto/Generated/*.cs (active version)
dotnet run --project Proto/Tool/ProtoOrganizer -- --active --compile --out Proto/4.4 --cs-out Proto/Generated
```

`protoc` is auto-located from the `Google.Protobuf.Tools` 3.33.1 NuGet cache; override with `--protoc <path>`.

The regrouping is guaranteed type-safe: it re-emits curated bodies verbatim, so the generated C# type
set, field numbers, and properties are identical to before — only the descriptor file grouping changes.

## Adding a new version

Drop a new dump's `StarRail.proto` / `packetIds.txt` / `translations.txt` into `Proto/<newVer>/Raw/`,
then run the organizer pointed at it. Names still obfuscated after grouping are reported for recovery
into the cross-version translation source (`ProtoBase/`, planned).
