# 每日实训回归

测试项目：[`scripts/DailyActiveRegression/`](../../scripts/DailyActiveRegression/)

从仓库根目录运行：

```powershell
dotnet run --project scripts/DailyActiveRegression/DailyActiveRegression.csproj -c Release
```

测试使用临时 SQLite 数据库与隔离配置，不启动网络服务，不读取运营配置或玩家数据库。默认读取当前 `Config/Resource.bin` 的副本；缓存必须包含 `DailyQuest`、`DailyMissionData`、`DailyMissionCount` 三张表以及 `CocoonConfig` 的 `CocoonType`/`FarmType` 字段，否则第一项检查失败并退出。可用 `--cache <路径>` 指定其他缓存；同时给出 `--stale-cache <旧缓存> --resources <原始资源目录>` 时，额外验证缺少上述表的旧缓存在原始 `DailyQuest.json` 可用时被拒绝。

覆盖范围：

- 日期键：服务器时区 03:59 属于前一天、04:00 开始新一天；刷新时刻 0 与跨月边界；`ServerOption.DailyResetHour` 越界时钳制到 0–23。
- 抽取：FNV-1a-64 + SplitMix64 + Fisher–Yates，结果与运行时无关；同一 (uid, 日期, nonce) 得到同一组 8 个，池顺序不影响结果，nonce 递增得到不同组合；已删除的 `DailyQuest` 行不会入池；QuestType 5 不再被条件自动接取。
- 活跃度与档位：领取实训任务奖励发放物品 23 并计入当日活跃度，`GetItem(23)` 返回当前值；未知档位 4101、重复领取 4102、活跃度不足 4103；一键领取按档位升序、奖励合并，重复一键领取不发放任何奖励。
- 跨日：新周期清空活跃度与已领档位；前一天未入选的实训任务关闭（未领奖励作废），当天入选的任务（含之前已领取的）重置为进行中、进度 0；`/daily reset` 等价的 nonce 重抽；`FinishQuest` 型实训（2100156/2100159）入选时其子任务重新激活，不会被昨日完成状态直接满足。
- 事件型完成条件：心跳（空参数）与无关参数不推进任何事件型实训；`AnyRelicLevelUp`、`AnyEquipmentLevelUp`、`AnyAvatarLevelUp`、`RogueFinishCnt`、`ResolveRelic`、`AnyItemComposeCount`、`ExpeditionSetOrTakeReward` 只由各自事件推进；`ChallengeFinishCnt` 按挑战类型过滤。
- 花萼与副本：战斗失败与凝滞虚影战斗（仅有 `CocoonWave`、无 `CocoonId`）不计数；金色花萼完成 `CocoonFinish` 与 `FarmTypeCocoonFinish [6,7,8]`，赤色花萼完成 `[5]`，侵蚀隧洞完成 `AnyCocoonFinish [3]`。
- 每日委托：`FinishTodayDailyMissionCnt` 在完成每日委托后达成，同一委托同一周期只计一次；跨日当日计数清零、累计计数保留。开启任务系统的玩家：未满足解锁任务时不发放，强制发放按 `DailyMissionCount.DailyCount` 补足，同日重复发放不追加；完成委托计入计数，跨日后已完成委托恢复为可接取并重新发放。
- 每日实训数据写入独立的 `DailyActive` 表。

边界：这不是客户端测试。面板显示、领取动画、任务追踪以及 `DailyActiveInfoNotify` 与 `GetDailyActiveInfoScRsp` 的客户端取值需要实机验证；`ExcellentEnterBattleAndWin`、`ItemCompose`、`UseMazeSkill`、`PropDestruct`、`WinBattleWithAssistAvatarCnt`、`TakePictureCnt`、`RogueExploreRoomFinishCnt`、`UseItemWithType` 等实训条件尚未实现，入选后无法完成；2100156 的货币战争子任务（`GridFightFinishGamePlay`）同样未实现，只能通过差分宇宙子任务完成。
