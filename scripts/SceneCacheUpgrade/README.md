# 场景缓存元数据升级

`IsHoyoGroup` 以追加的可空字段保存：旧缓存为 `null`，原始资源省略该字段时为 `false`。运行时拒绝服务端氛围组缺少该元数据的缓存，避免只更新程序后仍然加载错误的氛围组。发布时必须携带配套的 `Config/Resource.bin`。

如需保留已有缓存中的其他修改，可在有同版本原始资源的开发机执行：

```powershell
dotnet run --project scripts/SceneCacheUpgrade/SceneCacheUpgrade.csproj -c Release -- Config/Resource.bin Resources artifacts/Resource.scene-upgraded.bin
```

工具仅补入组的 `IsHoyoGroup`，不重建全部游戏资源，不读取运营配置或数据库。它先验证新快照还原该字段后与原快照完全一致，再写入新的输出文件；遇到服务端氛围组缺资源、异常数据或已存在输出文件会停止，绝不覆盖输入。旧缓存保留但当前资源已移除的客户端组或非氛围组会原样保留并报告数量，该标记不参与这些组的服务端加载筛选。

验证回归后，备份现有缓存，再将新文件复制为 `Config/Resource.bin`。回滚时程序和缓存需要一起恢复；旧程序不能读取新增字段的缓存。
