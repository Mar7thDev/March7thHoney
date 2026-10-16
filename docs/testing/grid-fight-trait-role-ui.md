# 羁绊详情角色列表排查（2026-10-13）

用户现象：左侧羁绊详情底部只有星徽，没有对应角色头像。

## 复测后的客户端修复

用户复测“连击”详情仍只有星徽，确认上次模块编号修改没有解决头像显示。本轮新增 `Lua/gridfight_trait_roles.lua`，在游戏内执行：

```text
/windy load gridfight_trait_roles.lua
```

随后关闭并重新打开羁绊详情。脚本通过已有命令即时加载，不需要重启服务端；客户端重新启动后需再次加载。本轮没有代替用户向运行中的客户端注入脚本。

脚本覆盖 `Ui.GridFight.Common.Trait.GridFightTraitCardPanel._get_trait_members`：保留原方法返回的全部成员，按当前客户端原始 `GridFightRoleConfig_Index_SeasonAndTrait.json` 的赛季 2 索引补充缺失角色。使用原生 `GridFightRole.CreateDummy(roleId)` 构造展示对象，继续由原面板判断持有、上场、灰色和高亮；不把展示角色添加到玩家队伍。已存在的角色按 ID 去重，原 NPC 和星徽入口保留，其他赛季不改动。

补齐表含 29 个羁绊、64 个独立角色 ID，与同版本 `GridFightRoleBasicInfo.json` 的赛季 2 角色集合一致。原生 `GetRolesByTrait` 内部为何返回缺失名单仍未定位；本轮修复的是客户端消费列表这条已确认路径，不声称改变了原生查询方法。

### 新增证据与检查

按 Luau 版本 4 的串行格式读取客户端字节码，而非只凭字符串推断：

- `GridFightTraitCardPanel` 模块字节范围 `22051026..22070909`，共 66 个函数原型。
- 原型 34（`_get_trait_members`）直接调用 `DataContext.TraitRepository:GetRolesByTrait(self._trait.ID)`，转入 `List<GridFightGridMember>`，游戏模式再追加 NPC/装备成员。
- `_init_members` 保存该列表，`_refresh_roles` 按其 Count 加星徽/光环数量设置成员列表长度，`_on_member_item_change` 将成员交给原有头像组件。
- 相邻 `GridFightSpotLightDetailRoleListPanel` 的原型 9 使用 `GridFightRole.CreateDummy(roleId)` 创建同类型的展示角色，证实本轮使用的工厂调用方式。
- 格式依据：[Luau 加载器](https://github.com/luau-lang/luau/blob/master/VM/src/lvmload.cpp)、[指令定义](https://github.com/luau-lang/luau/blob/master/Common/include/Luau/Bytecode.h)。分析文件位于 `tmp/trait-display/`。

`tmp/trait-display/test_patch.py` 使用本地 Lua 运行时和 C# 列表接口模拟完成 33 项检查：29 组索引补齐与去重、保留已有对象/NPC、重复加载、其他赛季不受影响、缺失客户端配置时保留面板并输出诊断。输出为 `tmp/trait-display/test.log`。这验证脚本逻辑和原始索引一致性；还需在实际客户端加载后确认截图中的角色头像出现。本轮无 C# 改动，因此没有重新构建服务端或重复运行无关战斗回归。

## 已确认与修改

- 当前缓存的64个赛季2角色均存在，且 `IsInBook` 为真；原始 `GridFightRoleConfig_Index_SeasonAndTrait.json` 有29个赛季2羁绊索引。因此没有向战斗成员列表塞入未拥有角色，也没有更改羁绊计数。
- 原 `ResolveContentModuleId` 直接取禁用表中最大的模块号7110501；这些表仍含旧赛季角色1508/1509、羁绊1013等内容。当前 `GridFightSeasonModule` 中赛季2、子赛季1的模块为7110101。
- 赛季2现在直接使用当前赛季模块；赛季1保留原选择行为。修复同时作用于客户端基础信息 `module_id_list` 和服务端按模块筛选的角色/投资/环境，避免两边使用不同模块。
- 回归检查当前赛季模块选择，以及序列化往返后的 `GridFightCurrentInfo` 基础信息；全量389/389通过。

## 客户端证据与限制

只读检查本地4.6.51客户端的Lua资源：

`Starrail 4.7/4.7v1整包/StarRail_4.6.51/StarRail_Data/StreamingAssets/Lua/Windows/7b5c63c29888c5a923093c1f132ac444.bytes`

字符串区约22053411附近包含 `GridFightTraitCardPanel`、`_get_trait_members`、`TraitRepository`、`GetRolesByTrait`、`_get_on_board_role`、`TeamRepository`，说明详情面板有独立的角色查询路径。约23618928附近包含 `GridFightPreparationTraitDetailPanel`。这些是静态符号证据，尚未反编译出原生 `GetRolesByTrait` 的完整筛选实现。

模块选择错误已经修正；是否足以解决截图中的头像缺失，仍需加载新版后由客户端实测，不能把服务端回归通过当成画面已经验证。若仍为空，下一步继续核对该原生查询方法及实际加载的客户端索引。

本轮没有重启服务器、修改客户端文件或玩家存档。源码备份在 `tmp/trait-ui-before`，检查输出在 `tmp/trait-ui-regression.log` 和 `tmp/trait-ui-build.log`。
