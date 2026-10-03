# 场景缓存元数据升级

升级工具：[`scripts/SceneCacheUpgrade/`](../../scripts/SceneCacheUpgrade/)

`IsHoyoGroup` 以追加的可空字段保存：旧缓存为 `null`，原始资源省略该字段时为 `false`。运行时拒绝服务端氛围组缺少该元数据的缓存，避免只更新程序后仍然加载错误的氛围组。发布时必须携带配套的 `Config/Resource.bin`。

如需保留已有缓存中的其他修改，可在有同版本原始资源的开发机执行：

```powershell
dotnet run --project scripts/SceneCacheUpgrade/SceneCacheUpgrade.csproj -c Release -- Config/Resource.bin Resources artifacts/Resource.scene-upgraded.bin
```

工具仅补入组的 `IsHoyoGroup`，不重建全部游戏资源，不读取运营配置或数据库。它先验证新快照还原该字段后与原快照完全一致，再写入新的输出文件；遇到服务端氛围组缺资源、异常数据或已存在输出文件会停止，绝不覆盖输入。旧缓存保留但当前资源已移除的客户端组或非氛围组会原样保留并报告数量，该标记不参与这些组的服务端加载筛选。

验证回归后，备份现有缓存，再将新文件复制为 `Config/Resource.bin`。回滚时程序和缓存需要一起恢复；旧程序不能读取新增字段的缓存。

## 差分宇宙数据升级

升级工具：[`scripts/RogueTournCacheUpgrade/`](../../scripts/RogueTournCacheUpgrade/)

差分宇宙 4.6 流程读取的 `RoguePersonaLayerRoom`、`RoguePersonaRoomPreset`、`RoguePersonaRoomCompType`、`RoguePersonaStyleGift`、`RogueTournExpReward`、事件 Act 图（`RogueDialogue/RogueEventTourn*/Event*/Act*.json`），以及 `RogueTournBuffGroup.TournMode` 列，都以追加成员保存在缓存中；旧缓存读取时这些成员为空。用同版本原始资源完整重建缓存时会自动包含它们；只需补这几项时可执行：

```powershell
dotnet run --project scripts/RogueTournCacheUpgrade/RogueTournCacheUpgrade.csproj -c Release -- Config/Resource.bin <资源目录> artifacts/Resource.tourn-upgraded.bin
```

资源目录只需包含上述 `ExcelOutput/*.json`、`Config/Level/Rogue/RogueNPC/` 与 `Config/Level/Rogue/RogueDialogue/RogueEventTourn*/`（例如 TurnBasedGameData 的 `OSPRODWin4.6.0` 提交的这些文件）。工具先确认原缓存尚未包含这些数据，再验证去掉新增内容后快照与原快照逐字节一致，最后写入新文件并重新读取校验；缺任何一个 Act 图会停止，绝不覆盖输入。原始 `RoguePersonaLayerRoom.json` 存在而缓存缺少这些表时，运行时会拒绝缓存。替换前用 `LoginSceneRegression -- --resource-cache <新文件>` 验证并备份原缓存；回滚时程序与缓存一起恢复。

经典模拟宇宙祝福修复后，`RogueBuffData` / `RogueBuffGroupData` 以 MemoryPack union 保存经典与差分宇宙子类型，缓存格式随之改变：修复前的缓存无法被当前程序或本工具读取（日志报 `MemoryPackUnion` 标签错误，服务端随后尝试从原始资源重建）。此类缓存不能用本工具升级，需用同版本完整原始资源重新生成；仅部署缓存的环境必须同时更新程序与 `Config/Resource.bin`，回滚时同样一起恢复。
