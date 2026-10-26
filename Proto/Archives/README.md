# Archived protocol references

The inactive `4.4`, `4.5`, and `4.5v3` protocol references are stored in lossless ZIP archives. Normal server builds use active `4.6` and need no extraction. `Proto/4.5v3/translations.txt` is deliberately retained outside the ZIP because LoginSceneRegression reads it directly.

Each archive preserves the version directory, relative filenames, file bytes and Unix permission bits. Entries are sorted with a fixed 1980 timestamp. Every entry was compared byte-for-byte against the original Git blob before the original was removed.

## Restore for migration

Extract into `Proto/`, not into this directory. Never overwrite edited files.

Linux/macOS, from repository root:

```sh
unzip -n Proto/Archives/4.4.zip -d Proto
unzip -n Proto/Archives/4.5.zip -d Proto
unzip -n Proto/Archives/4.5v3.zip -d Proto
```

Windows PowerShell (omit `-Force` to preserve existing files):

```powershell
Expand-Archive -LiteralPath Proto/Archives/4.4.zip -DestinationPath Proto
Expand-Archive -LiteralPath Proto/Archives/4.5.zip -DestinationPath Proto
Expand-Archive -LiteralPath Proto/Archives/4.5v3.zip -DestinationPath Proto
```

A ZIP utility's Extract operation also works; choose `Proto/` as destination. Expanded migration references are ignored except for the intentionally retained translations file. Keep modified migration work separately or intentionally rebuild the archive after verification, rather than force-adding expanded trees.

ProtoOrganizer's legacy defaults refer to `Proto/4.4`; extract it before using defaults or supply explicit active paths. The historical 4.5 example in [the protocol guide](../../docs/development/protobuf.md) requires both 4.4 and 4.5. Provenance references in 4.6 overrides do not require extraction for normal builds.

## Contents

| ZIP | Files | Original bytes | Compressed bytes | SHA-256 |
| --- | ---: | ---: | ---: | --- |
| 4.4.zip | 176 | 3260769 | 944490 | 85948b7b5a11c6faa56af3d4f1a937d62a36937436986d9d545aecd858b26284 |
| 4.5.zip | 158 | 2211561 | 591808 | f1e0808624798f13f48d000cd12abc396e51fe6e7843201f8eb722ae5fe15799 |
| 4.5v3.zip | 10 | 2227157 | 609624 | 0c8146856475dff215ba44bf449b722a1e9b050671e2685acb1b927defc7da27 |

This saves working-tree space only. Original copies remain in Git history.
