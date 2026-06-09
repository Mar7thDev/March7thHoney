using March7thHoney.Data;
using March7thHoney.Data.Excel;
using March7thHoney.Database.Challenge;
using March7thHoney.Database.Inventory;
using March7thHoney.Database.Lineup;
using March7thHoney.GameServer.Game.Challenge.Instances;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server.Packet.Send.ChallengePeak;
using March7thHoney.Proto;
using March7thHoney.GameServer.Game.Challenge;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Game.ChallengePeak;

/// <summary>
///     this class is used to manage the challenge peak for a player
///     but the challenge instance shouldnt be stored here ( see ChallengeManager )
/// </summary>
/// <see cref="March7thHoney.GameServer.Game.Challenge.ChallengeManager" />
public class ChallengePeakManager(PlayerInstance player) : BasePlayerManager(player)
{
    public bool BossIsHard { get; set; } = false;

    public ChallengePeakGroup GetChallengePeakInfo(int groupId)
    {
        var proto = new ChallengePeakGroup
        {
            PeakGroupId = (uint)groupId,
            // Captured working accounts that show the 普通/绝境 toggle pair DisableHardMode=true with
            // both boss difficulties marked cleared (EasyMode/HardMode HasPassed below). Setting this
            // alone (without the HasPassed flags) instead locked the boss to 普通-only — the toggle
            // needs both. Private-server convenience: both difficulties selectable from the start,
            // no "clear all 3 knights first" gate.
            DisableHardMode = true
        };

        var data = GameData.ChallengePeakGroupConfigData.GetValueOrDefault(groupId);
        if (data == null) return proto;

        var starNum = 0;
        foreach (var levelId in data.PreLevelIDList)
        {
            var levelData = GameData.ChallengePeakConfigData.GetValueOrDefault(levelId);
            if (levelData == null) continue;

            var levelProto = new Proto.ChallengePeak
            {
                PeakId = (uint)levelId,
                HasPassed = true
            };

            if (Player.ChallengeManager!.ChallengeData.PeakLevelDatas.TryGetValue(levelId, out var levelPbData))
            {
                starNum += (int)levelPbData.PeakStar;

                levelProto.CyclesUsed = levelPbData.RoundCnt;
                levelProto.PeakAvatarIdList.AddRange(levelPbData.BaseAvatarList);
                levelProto.FinishedTargetList.AddRange(levelPbData.FinishedTargetList);
            }

            proto.Peaks.Add(levelProto);
        }

        // The client gates the boss 普通/绝境 toggle behind "3 knights cleared" (judged by star
        // count, not the per-peak HasPassed flag). Report full stars so both difficulties are
        // selectable from the start — captured toggle-enabled accounts carry obtainedStars == peaks*3.
        proto.ObtainedStars = (uint)Math.Max(starNum, proto.Peaks.Count * 3);
        proto.CountOfPeaks = (uint)proto.Peaks.Count;
        if (Player.ChallengeManager!.ChallengeData.PeakTakenRewardIds.TryGetValue(groupId, out var takenRewards))
            proto.TakenStarRewards.AddRange(takenRewards.Select(x => (uint)x));

        // boss
        var bossLevelId = data.BossLevelID;
        if (bossLevelId <= 0) return proto;

        var bossLevelData = GameData.ChallengePeakBossConfigData.GetValueOrDefault(bossLevelId);
        if (bossLevelData == null) return proto;

        var bossProto = new Proto.ChallengePeakBoss
        {
            BFBHCKEJDIG = (uint)bossLevelId,
            HardModeHasPassed = true,
            // Mark both difficulties unlocked even with no clearance record, so the client always
            // offers the 普通/绝境 switch from the start (real records below override with avatars/cycles).
            EasyMode = new ChallengePeakBossClearance { HasPassed = true },
            HardMode = new ChallengePeakBossClearance { HasPassed = true }
        };

        HashSet<uint> targetIds = [];
            if (Player.ChallengeManager!.ChallengeData.PeakBossLevelDatas.TryGetValue((bossLevelId << 2) | 0,
                out var bossPbData)) // easy (is hard = 0)
        {
            bossProto.EasyMode.PeakAvatarIdList.AddRange(bossPbData.BaseAvatarList);
            bossProto.EasyMode.BestCycleCount = bossPbData.RoundCnt;
            bossProto.EasyMode.HasPassed = true;
            bossProto.EasyMode.BuffId = bossPbData.BuffId;

            foreach (var targetId in bossPbData.FinishedTargetList) targetIds.Add(targetId);
        }

            if (Player.ChallengeManager!.ChallengeData.PeakBossLevelDatas.TryGetValue((bossLevelId << 2) | 1,
                out var bossHardPbData)) // easy (is hard = 1)
        {
            bossProto.HardModeHasPassed = true;
            bossProto.HardMode.PeakAvatarIdList.AddRange(bossHardPbData.BaseAvatarList);
            bossProto.HardMode.BestCycleCount = bossHardPbData.RoundCnt;
            bossProto.HardMode.HasPassed = true;
            bossProto.HardMode.BuffId = bossHardPbData.BuffId;

            foreach (var targetId in bossHardPbData.FinishedTargetList) targetIds.Add(targetId);
        }

        // No real clearance record yet: seed the full target set (normal + hard) so the boss reads as
        // cleared on both difficulties — matches captured accounts that expose the 普通/绝境 toggle.
        if (targetIds.Count == 0)
        {
            if (GameData.ChallengePeakConfigData.TryGetValue(bossLevelId, out var bossPeakCfg))
                foreach (var t in bossPeakCfg.NormalTargetList)
                    targetIds.Add((uint)t);
            if (bossLevelData.HardTarget > 0) targetIds.Add((uint)bossLevelData.HardTarget);
        }

        bossProto.FinishedTargetList.AddRange(targetIds);
        proto.PeakBoss = bossProto;

        return proto;
    }

    public async ValueTask<List<ChallengePeakRewardGroup>?> TakeRewards(int groupId, IEnumerable<uint> requestRewardIds)
    {
        if (!GameData.ChallengePeakGroupConfigData.TryGetValue(groupId, out var groupConfig)) return null;
        if (!GameData.ChallengePeakRewardData.TryGetValue(groupConfig.RewardGroupID, out var rewardLine)) return null;

        var passedMobs = groupConfig.PreLevelIDList.Count;
        var mobStars = passedMobs * 3;
        var hasBoss = groupConfig.BossLevelID > 0;
        var bossStars = hasBoss ? 3 : 0;
        var bossHardStars = hasBoss ? 3 : 0;

        if (!Player.ChallengeManager!.ChallengeData.PeakTakenRewardIds.TryGetValue(groupId, out var takenList))
        {
            takenList = [];
            Player.ChallengeManager.ChallengeData.PeakTakenRewardIds[groupId] = takenList;
        }

        var requested = requestRewardIds.Select(x => (int)x).ToHashSet();
        if (requested.Count == 0) requested = rewardLine.Select(x => x.ID).ToHashSet();

        var rspGroups = new List<ChallengePeakRewardGroup>();
        var gainItems = new List<ItemData>();

        foreach (var entry in rewardLine.OrderBy(x => x.ID))
        {
            if (!requested.Contains(entry.ID)) continue;
            if (takenList.Contains(entry.ID)) continue;

            var passed = entry.RewardType switch
            {
                "MOB_PASS_REWARD" => passedMobs >= entry.TypeValue,
                "MOB_STAR_REWARD" => mobStars >= entry.TypeValue,
                "BOSS_STAR_REWARD" => bossStars >= entry.TypeValue,
                "BOSS_STAR_LIMIT_REWARD" => bossHardStars >= entry.TypeValue,
                "BOSS_COLOR_TARGET_REWARD" => hasBoss,
                _ => false
            };
            if (!passed) continue;

            if (!GameData.RewardDataData.TryGetValue(entry.RewardID, out var rewardExcel)) continue;

            var group = new ChallengePeakRewardGroup
            {
                RewardId = (uint)entry.ID,
                Reward = new ItemList()
            };

            if (rewardExcel.Hcoin > 0)
            {
                var hcoin = new ItemData { ItemId = 1, Count = rewardExcel.Hcoin };
                gainItems.Add(hcoin);
                group.Reward.ItemList_.Add(hcoin.ToProto());
            }

            foreach (var (itemId, count) in rewardExcel.GetItems())
            {
                var item = new ItemData
                {
                    ItemId = itemId,
                    Count = count
                };
                gainItems.Add(item);
                group.Reward.ItemList_.Add(item.ToProto());
            }

            takenList.Add(entry.ID);
            rspGroups.Add(group);
        }

        if (gainItems.Count > 0) await Player.InventoryManager!.AddItems(gainItems);
        return rspGroups;
    }

    private int GetObtainedStars(ChallengePeakGroupConfigExcel groupConfig)
    {
        var stars = 0;
        foreach (var levelId in groupConfig.PreLevelIDList)
            if (Player.ChallengeManager!.ChallengeData.PeakLevelDatas.TryGetValue(levelId, out var levelData))
                stars += (int)levelData.PeakStar;

        if (groupConfig.BossLevelID > 0)
        {
            var easyKey = (groupConfig.BossLevelID << 2) | 0;
            var hardKey = (groupConfig.BossLevelID << 2) | 1;
            if (Player.ChallengeManager!.ChallengeData.PeakBossLevelDatas.TryGetValue(easyKey, out var easyData))
                stars += (int)easyData.PeakStar;
            if (Player.ChallengeManager!.ChallengeData.PeakBossLevelDatas.TryGetValue(hardKey, out var hardData))
                stars += (int)hardData.PeakStar;
        }

        return stars;
    }

    public async ValueTask SetLineupAvatars(int groupId, List<ChallengePeakLineup> lineups)
    {
        var datas = Player.ChallengeManager!.ChallengeData.PeakLevelDatas;
        foreach (var lineup in lineups)
        {
            List<uint> avatarIds = [];

            foreach (var avatarId in lineup.PeakAvatarIdList.ToList())
            {
                var avatar = Player.AvatarManager!.GetFormalAvatar((int)avatarId);
                if (avatar != null)
                    avatarIds.Add((uint)avatar.BaseAvatarId);
            }

            datas[(int)lineup.PeakId] = new ChallengePeakLevelData
            {
                LevelId = (int)lineup.PeakId,
                BaseAvatarList = avatarIds
            }; // reset
        }

        await Player.SendPacket(new PacketChallengePeakGroupDataUpdateScNotify(GetChallengePeakInfo(groupId)));
    }

    public async ValueTask SaveHistory(ChallengePeakInstance inst, List<uint> targetIds)
    {
        if (inst.Config.BossExcel != null)
        {
            // is hard
            var isHard = inst.Data.Peak!.IsHard;
            var levelId = ((int)inst.Data.Peak!.CurrentPeakLevelId << 2) | (isHard ? 1 : 0);

            // get old data
            if (Player.ChallengeManager!.ChallengeData.PeakBossLevelDatas.TryGetValue(levelId, out var oldData) &&
                oldData.FinishedTargetList.Count > targetIds.Count && oldData.RoundCnt < inst.Data.Peak!.RoundCnt)
                // better data already exists, do not overwrite
                return;

            // Save boss data
            var data = new ChallengePeakBossLevelData
            {
                LevelId = (int)inst.Data.Peak!.CurrentPeakLevelId,
                IsHard = isHard,
                BaseAvatarList = Player.LineupManager!.GetCurLineup()?.BaseAvatars?.Select(x => (uint)x.BaseAvatarId)
                    .ToList() ?? [],
                RoundCnt = inst.Data.Peak!.RoundCnt,
                BuffId = inst.Data.Peak!.Buffs.FirstOrDefault(),
                FinishedTargetList = targetIds,
                PeakStar = (uint)targetIds.Count
            };

            Player.ChallengeManager!.ChallengeData.PeakBossLevelDatas[levelId] = data;

            // set head frame
            if (isHard)
            {
                await Player.SetPlayerHeadFrameId(GameConstants.CHALLENGE_PEAK_ULTRA_FRAME_ID, long.MaxValue);
            }
            else
            {
                var targetFrameId = data.PeakStar + 226000;
                if (Player.Data.HeadFrame.HeadFrameId < targetFrameId)
                    await Player.SetPlayerHeadFrameId(targetFrameId, long.MaxValue);
            }
        }
        else
        {
            // Save level data
            var levelId = (int)inst.Data.Peak!.CurrentPeakLevelId;

            // get old data
            if (Player.ChallengeManager!.ChallengeData.PeakLevelDatas.TryGetValue(levelId, out var oldData) &&
                oldData.FinishedTargetList.Count > targetIds.Count && oldData.RoundCnt < inst.Data.Peak!.RoundCnt)
                // better data already exists, do not overwrite
                return;

            var data = new ChallengePeakLevelData
            {
                LevelId = levelId,
                BaseAvatarList = Player.LineupManager!.GetCurLineup()?.BaseAvatars?.Select(x => (uint)x.BaseAvatarId)
                    .ToList() ?? [],
                RoundCnt = inst.Data.Peak!.RoundCnt,
                FinishedTargetList = targetIds,
                PeakStar = (uint)targetIds.Count
            };

            Player.ChallengeManager!.ChallengeData.PeakLevelDatas[levelId] = data;
        }

        await Player.SendPacket(
            new PacketChallengePeakGroupDataUpdateScNotify(
                GetChallengePeakInfo((int)inst.Data.Peak!.CurrentPeakGroupId)));
    }

    public async ValueTask StartChallenge(int levelId, uint buffId, List<int> avatarIdList)
    {
        // Get challenge excel
        if (!GameData.ChallengePeakConfigData.TryGetValue(levelId, out var excel))
        {
            await Player.SendPacket(new PacketStartChallengePeakScRsp(Retcode.RetChallengeNotExist));
            return;
        }

        // Format to base avatar id
        List<int> avatarIds = [];
        foreach (var avatarId in avatarIdList)
        {
            var avatar = Player.AvatarManager!.GetFormalAvatar(avatarId);
            if (avatar != null)
                avatarIds.Add(avatar.BaseAvatarId);
        }

        // Get lineup
        var lineup = Player.LineupManager!.GetExtraLineup(ExtraLineupType.LineupChallenge);
        if (lineup == null)
        {
            // Ensure challenge extra lineup exists before filling avatars.
            Player.LineupManager.SetExtraLineup(ExtraLineupType.LineupChallenge, []);
            lineup = Player.LineupManager.GetExtraLineup(ExtraLineupType.LineupChallenge);
        }

        if (lineup == null)
        {
            await Player.SendPacket(new PacketStartChallengePeakScRsp(Retcode.RetChallengeLineupEmpty));
            return;
        }
        if (avatarIds.Count > 0)
            lineup.BaseAvatars = avatarIds.Select(x => new LineupAvatarInfo
            {
                BaseAvatarId = x
            }).ToList();
        else
            lineup.BaseAvatars = Player.ChallengeManager!.ChallengeData.PeakLevelDatas.GetValueOrDefault(levelId)
                ?.BaseAvatarList
                .Select(x => new LineupAvatarInfo
                {
                    BaseAvatarId = (int)x
                }).ToList() ?? [];

        // Set technique points to full
        lineup.Mp = 8; // Max Mp

        // Make sure this lineup has avatars set
        if (Player.AvatarManager!.AvatarData.FormalAvatars.Count == 0)
        {
            await Player.SendPacket(new PacketStartChallengePeakScRsp(Retcode.RetChallengeLineupEmpty));
            return;
        }

        // Reset hp/sp
        foreach (var avatar in Player.AvatarManager!.AvatarData.FormalAvatars)
        {
            avatar.SetCurHp(10000, true);
            avatar.SetCurSp(5000, true);
        }

        // Set challenge data for player
        var data = new ChallengeStateData
        {
            Peak = new PeakChallengeState
            {
                CurrentPeakGroupId = (uint)(GameData.ChallengePeakGroupConfigData.Values
                    .FirstOrDefault(x => x.BossLevelID == levelId || x.PreLevelIDList.Contains(levelId))?.ID ??
                    GameData.GetCurrentChallengePeakGroupId()),
                CurrentPeakLevelId = (uint)levelId,
                CurrentExtraLineup = ChallengeLineupType.Challenge1,
                CurStatus = 1
            }
        };

        if (excel.BossExcel != null)
            data.Peak!.IsHard = BossIsHard;

        if (buffId > 0) data.Peak!.Buffs.Add(buffId);

        var instance = new ChallengePeakInstance(Player, data);

        Player.ChallengeManager!.ChallengeInstance = instance;

        // Set first lineup before we enter scenes
        await Player.LineupManager!.SetExtraLineup((ExtraLineupType)instance.Data.Peak!.CurrentExtraLineup);

        // Enter scene
        try
        {
            var mapEntranceId = excel.GetMapEntranceId();
            if (mapEntranceId <= 0)
            {
                mapEntranceId = GameData.ChallengePeakGroupConfigData.Values
                    .FirstOrDefault(x => x.BossLevelID == levelId || x.PreLevelIDList.Contains(levelId))
                    ?.MapEntranceID ?? 0;
            }

            if (mapEntranceId <= 0)
            {
                await Player.SendPacket(new PacketStartChallengePeakScRsp(Retcode.RetChallengeNotExist));
                return;
            }

            await Player.EnterScene(mapEntranceId, 0, true);
        }
        catch
        {
            // Reset lineup/instance if entering scene failed
            Player.ChallengeManager!.ChallengeInstance = null;

            // Send error packet
            await Player.SendPacket(new PacketStartChallengePeakScRsp(Retcode.RetChallengeNotExist));
            return;
        }

        // Save start positions
        data.Peak!.StartPos = Player.Data.Pos!;
        data.Peak!.StartRot = Player.Data.Rot!;
        data.Peak!.SavedMp = (uint)Player.LineupManager.GetCurLineup()!.Mp;

        // Send packet
        await Player.SendPacket(new PacketStartChallengePeakScRsp(Retcode.RetSucc));

        // Save instance
        Player.ChallengeManager!.SaveInstance(instance);
    }
}
