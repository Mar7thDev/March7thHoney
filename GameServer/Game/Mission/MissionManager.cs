using March7thHoney.Data;
using March7thHoney.Data.Config;
using March7thHoney.Data.Excel;
using March7thHoney.Database;
using March7thHoney.Database.Inventory;
using March7thHoney.Enums.Item;
using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.Battle;
using March7thHoney.GameServer.Game.Mission.FinishAction;
using March7thHoney.GameServer.Game.Mission.FinishType;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Plugin.Event;
using March7thHoney.GameServer.Server.Packet.Send.HeartDial;
using March7thHoney.GameServer.Server.Packet.Send.Mission;
using March7thHoney.GameServer.Server.Packet.Send.PlayerSync;
using March7thHoney.Proto;
using March7thHoney.Util;
using System.Linq;
using MissionData = March7thHoney.Database.Quests.MissionData;

namespace March7thHoney.GameServer.Game.Mission;

public class MissionManager(PlayerInstance player) : BasePlayerManager(player)
{
    #region Initializer & Properties

    public MissionData Data { get; set; } = DatabaseHelper.Instance!.GetInstanceOrCreateNew<MissionData>(player.Uid);
    public static readonly Dictionary<FinishActionTypeEnum, MissionFinishActionHandler> ActionHandlers = [];
    public static readonly Dictionary<MissionFinishTypeEnum, MissionFinishTypeHandler> FinishTypeHandlers = [];

    public readonly List<int> SkipSubMissionList = []; // bug

    #endregion

    private void MarkMissionDataDirty()
    {
        DatabaseHelper.ToSaveUidList.SafeAdd(Player.Uid);
    }

    private async ValueTask RefreshMissionState(bool syncScene = true, bool syncStoryLine = true, bool syncQuest = true)
    {
        await Player.SendPacket(new PacketGetMissionDataScRsp(Player));
        await Player.SendPacket(new PacketGetMissionStatusScRsp(new GetMissionStatusCsReq
        {
            MainMissionIdList = { GameData.MainMissionData.Keys.Select(x => (uint)x) },
            SubMissionIdList = { GameData.SubMissionInfoData.Keys.Select(x => (uint)x) }
        }, Player));

        if (syncScene)
            Player.SceneInstance?.SyncGroupInfo();

        if (syncStoryLine && Player.StoryLineManager != null)
        {
            await Player.StoryLineManager.CheckIfFinishStoryLine();
            await Player.StoryLineManager.CheckIfEnterStoryLine();
        }

        if (syncQuest && Player.QuestManager != null)
            await Player.QuestManager.SyncQuest();
    }

    #region Mission Actions

    public List<int> GetAllFinishedMissionIds()
    {
        return Data.FinishedSubMissionIds
            .Concat(Data.FinishedMainMissionIds)
            .Where(x => x > 0)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
    }

    public async ValueTask<List<MissionSync?>> AcceptMainMission(int missionId, bool sendPacket = true)
    {
        if (!ConfigManager.Config.ServerOption.EnableMission) return [];
        if (Data.GetMainMissionStatus(missionId) != MissionPhaseEnum.None) return []; // already accepted
        // Get entry sub mission
        GameData.MainMissionData.TryGetValue(missionId, out var mission);
        if (mission == null) return [];

        if (mission.MissionInfo != null && mission.SubMissionIds.Count == 0)
        {
            ResourceManager.TryPopulateMissionInfoFromMissionPack(mission, mission.MissionInfo);
            mission.SetMissionInfo(mission.MissionInfo);
        }

        Data.SetMainMissionStatus(missionId, MissionPhaseEnum.Accept);
        MarkMissionDataDirty();

        var list = new List<MissionSync?>();
        foreach (var subMissionId in mission.MissionInfo?.StartSubMissionList ?? [])
            list.Add(await AcceptSubMission(subMissionId, sendPacket));

        if (missionId == 4030001 || missionId == 4030002)
        {
            // skip  not implemented
            foreach (var subMission in mission.MissionInfo?.SubMissionList ?? [])
                await AcceptSubMission(subMission.ID);

            foreach (var subMission in mission.MissionInfo?.SubMissionList ?? [])
                await FinishSubMission(subMission.ID);
        }

        if (missionId == 1000400)
        {
            await Player.AddAvatar(1003);
            await Player.LineupManager!.AddAvatarToCurTeam(1003);
        }

        // message
        foreach (var sectionConfigExcel in GameData.MessageSectionConfigData.Values.Where(x =>
                     x.MainMissionLink == missionId))
            await Player.MessageManager!.AddMessageSection(sectionConfigExcel.ID);

        foreach (var info in mission.MissionInfo!.SubMissionList.Where(x =>
                     x.FinishType is MissionFinishTypeEnum.MessagePerformSectionFinish
                         or MissionFinishTypeEnum.MessageSectionFinish))
            await Player.MessageManager!.AddMessageSection(info.ParamInt1);

        return list;
    }

    public async ValueTask<MissionSync> AcceptMainMissionByCondition(bool sendPacket = true)
    {
        var sync = new MissionSync();
        var newlyAcceptedMainMissionIds = new List<int>();
        foreach (var nextMission in GameData.MainMissionData.Values)
        {
            if (!nextMission.IsEqual(Data)) continue;
            if (Data.GetMainMissionStatus(nextMission.MainMissionID) != MissionPhaseEnum.None)
                continue; // already accepted
            var res = await AcceptMainMission(nextMission.MainMissionID, sendPacket);
            newlyAcceptedMainMissionIds.Add(nextMission.MainMissionID);
            foreach (var subMission in res)
                if (subMission != null)
                    sync.MissionList.AddRange(subMission.MissionList);
        }

        if (Data.TrackingMainMissionId == 0 && newlyAcceptedMainMissionIds.Count > 0)
        {
            var nextTrackMissionId = newlyAcceptedMainMissionIds
                .OrderBy(id => GameData.MainMissionData.GetValueOrDefault(id)?.GetId() ?? int.MaxValue)
                .First();
            Data.TrackingMainMissionId = nextTrackMissionId;
            MarkMissionDataDirty();
        }

        return sync;
    }

    public async ValueTask<List<MissionSync?>> ReAcceptMainMission(int missionId, bool sendPacket = true)
    {
        if (!ConfigManager.Config.ServerOption.EnableMission) return [];

        GameData.MainMissionData.TryGetValue(missionId, out var mission);
        if (mission == null) return [];

        MissionSync sync = new();

        foreach (var subMission in mission.SubMissionIds)
            if (Data.GetSubMissionStatus(subMission) == MissionPhaseEnum.Finish ||
                Data.GetSubMissionStatus(subMission) == MissionPhaseEnum.Accept)
                sync.MissionList.Add(new Proto.Mission
                {
                    Id = (uint)subMission,
                    Status = MissionStatus.MissionNone
                });

        foreach (var subMission in mission.SubMissionIds)
        {
            Data.SetSubMissionStatus(subMission, MissionPhaseEnum.None);
            Data.SubMissionProgressDict.Remove(subMission);
        }

        Data.SetMainMissionStatus(missionId, MissionPhaseEnum.None);
        if (Data.TrackingMainMissionId == missionId)
            Data.TrackingMainMissionId = 0;
        MarkMissionDataDirty();

        await Player.SendPacket(new PacketPlayerSyncScNotify(sync));

        var result = await AcceptMainMission(missionId, sendPacket);
        Data.TrackingMainMissionId = missionId;
        if (sendPacket)
            await Player.SendPacket(new PacketAcceptMainMissionScRsp((uint)missionId));
        await RefreshMissionState();
        await Player.SendPacket(new PacketUpdateTrackMainMissionIdScRsp(0, missionId));

        return result;
    }

    public async ValueTask RemoveMainMission(int missionId)
    {
        if (!ConfigManager.Config.ServerOption.EnableMission) return;
        Data.SetMainMissionStatus(missionId, MissionPhaseEnum.None);
        MarkMissionDataDirty();

        GameData.MainMissionData.TryGetValue(missionId, out var mission);
        if (mission == null) return;

        MissionSync sync = new();

        foreach (var subMission in mission.SubMissionIds)
        {
            Data.SetSubMissionStatus(subMission, MissionPhaseEnum.None);
            await SetMissionProgress(subMission, 0, false);
            sync.MissionList.Add(new Proto.Mission
            {
                Id = (uint)subMission,
                Status = MissionStatus.MissionNone
            });
        }

        await Player.SendPacket(new PacketPlayerSyncScNotify(sync));
    }

    public async ValueTask AcceptSubMission(int missionId)
    {
        if (!ConfigManager.Config.ServerOption.EnableMission) return;
        await AcceptSubMission(missionId, true);
    }

    public async ValueTask<MissionSync?> AcceptSubMission(int missionId, bool sendPacket,
        bool doFinishTypeAction = true)
    {
        if (!ConfigManager.Config.ServerOption.EnableMission) return null;
        GameData.SubMissionInfoData.TryGetValue(missionId, out var mission);
        if (mission == null) return null;
        if (Data.GetSubMissionStatus(missionId) != MissionPhaseEnum.None) return null; // already accepted

        Data.SetSubMissionStatus(missionId, MissionPhaseEnum.Accept);
        MarkMissionDataDirty();

        var sync = new MissionSync();
        sync.MissionList.Add(new Proto.Mission
        {
            Id = (uint)missionId,
            Status = MissionStatus.MissionDoing
        });

        if (sendPacket) await Player.SendPacket(new PacketPlayerSyncScNotify(sync));
        Player.SceneInstance?.SyncGroupInfo();
        if (mission.SubMissionInfo != null)
            try
            {
                FinishTypeHandlers.TryGetValue(mission.SubMissionInfo.FinishType, out var handler);
                if (doFinishTypeAction)
                    if (handler != null)
                        await handler.HandleMissionFinishType(Player, mission.SubMissionInfo, null);
            }
            catch
            {
            }

        if (SkipSubMissionList.Contains(missionId)) await FinishSubMission(missionId);

        if (mission.SubMissionInfo?.LevelFloorID == Player.SceneInstance?.FloorId)
            if (mission.SubMissionInfo?.GroupIDList != null)
                foreach (var group in mission.SubMissionInfo.GroupIDList)
                    await Player.SceneInstance!.EntityLoader!.LoadGroup(group);

        // TODO: Mission Task
        Player.TaskManager?.MissionTaskTrigger.TriggerMissionTask(missionId);

        return sync;
    }

    public async ValueTask FinishMainMission(int missionId)
    {
        if (!ConfigManager.Config.ServerOption.EnableMission) return;
        if (!GameData.MainMissionData.TryGetValue(missionId, out var mainMission)) return;
        if (Data.GetMainMissionStatus(missionId) != MissionPhaseEnum.Accept) return;
        Data.SetMainMissionStatus(missionId, MissionPhaseEnum.Finish);
        MarkMissionDataDirty();
        var finishedMissionIds = new List<int> { missionId };
        var sync = new MissionSync();
        sync.FinishedMainMissionIdList.Add((uint)missionId);
        // get next main mission
        foreach (var mission in mainMission.SubMissionIds)
            if (GetSubMissionStatus(mission) != MissionPhaseEnum.Finish)
                if (Data.GetSubMissionStatus(mission) != MissionPhaseEnum.Finish)
                {
                    Data.SetSubMissionStatus(mission, MissionPhaseEnum.Finish);
                    finishedMissionIds.Add(mission);
                    sync.MissionList.Add(new Proto.Mission
                    {
                        Id = (uint)mission,
                        Status = MissionStatus.MissionFinish
                    });
                }

        var mainSync = await AcceptMainMissionByCondition(false);
        sync.MissionList.AddRange(mainSync.MissionList);

        if (mainMission.NextTrackMainMission > 0 &&
            Data.GetMainMissionStatus(mainMission.NextTrackMainMission) == MissionPhaseEnum.None)
        {
            var nextMainSync = await AcceptMainMission(mainMission.NextTrackMainMission, false);
            foreach (var subMissionSync in nextMainSync)
                if (subMissionSync != null)
                    sync.MissionList.AddRange(subMissionSync.MissionList);
        }

        if (Data.TrackingMainMissionId == 0 && GameData.MainMissionData.TryGetValue(missionId, out var finishedMain) &&
            finishedMain.NextTrackMainMission > 0 &&
            GetMainMissionStatus(finishedMain.NextTrackMainMission) == MissionPhaseEnum.Accept)
            Data.TrackingMainMissionId = finishedMain.NextTrackMainMission;

        await Player.SendPacket(new PacketPlayerSyncScNotify(sync));
        await Player.SendPacket(new PacketStartFinishMainMissionScNotify(missionId));
        await Player.SendPacket(new PacketFinishedMissionScNotify(finishedMissionIds));
        await Player.SendPacket(new PacketGetMissionDataScRsp(Player));
        await Player.SendPacket(new PacketGetMissionStatusScRsp(new GetMissionStatusCsReq
        {
            MainMissionIdList = { GameData.MainMissionData.Keys.Select(x => (uint)x) },
            SubMissionIdList = { GameData.SubMissionInfoData.Keys.Select(x => (uint)x) }
        }, Player));
        await HandleMissionReward(missionId);
        await HandleFinishType(MissionFinishTypeEnum.FinishMission);

        await Player.RaidManager!.CheckIfLeaveRaid();

        PluginEvent.InvokeOnPlayerFinishMainMission(Player, missionId);
    }

    public async ValueTask<bool> ForceFinishMainMission(int missionId)
    {
        if (!ConfigManager.Config.ServerOption.EnableMission) return false;
        if (!GameData.MainMissionData.TryGetValue(missionId, out var mainMission)) return false;

        var status = Data.GetMainMissionStatus(missionId);
        if (status == MissionPhaseEnum.Finish) return false;

        Data.SetMainMissionStatus(missionId, MissionPhaseEnum.Accept);
        if (Data.TrackingMainMissionId == 0)
            Data.TrackingMainMissionId = missionId;
        MarkMissionDataDirty();

        foreach (var subMissionId in mainMission.SubMissionIds)
        {
            Data.SetSubMissionStatus(subMissionId, MissionPhaseEnum.Finish);
            Data.SubMissionProgressDict[subMissionId] =
                GameData.SubMissionInfoData.GetValueOrDefault(subMissionId)?.SubMissionInfo?.Progress ?? 1;
        }

        Data.SetMainMissionStatus(missionId, MissionPhaseEnum.Finish);
        MarkMissionDataDirty();

        var nextTrackMissionId = mainMission.NextTrackMainMission;
        if (nextTrackMissionId > 0 && Data.GetMainMissionStatus(nextTrackMissionId) == MissionPhaseEnum.None)
            await AcceptMainMission(nextTrackMissionId, false);

        if (Data.TrackingMainMissionId == missionId)
        {
            if (nextTrackMissionId > 0 && Data.GetMainMissionStatus(nextTrackMissionId) == MissionPhaseEnum.Accept)
                Data.TrackingMainMissionId = nextTrackMissionId;
            else
                Data.TrackingMainMissionId = 0;
            MarkMissionDataDirty();
        }

        await AcceptMainMissionByCondition(false);
        await RefreshMissionState(syncScene: false, syncStoryLine: false, syncQuest: false);
        return Data.GetMainMissionStatus(missionId) == MissionPhaseEnum.Finish;
    }

    public async ValueTask FinishSubMission(int missionId)
    {
        if (!ConfigManager.Config.ServerOption.EnableMission) return;
        GameData.SubMissionInfoData.TryGetValue(missionId, out var subMission);
        if (subMission == null) return;
        var mainMissionId = subMission.MainMissionId;
        if (Data.GetSubMissionStatus(missionId) != MissionPhaseEnum.Accept) return; // not accepted
        GameData.MainMissionData.TryGetValue(mainMissionId, out var mainMission); // get main mission
        if (mainMission == null) return;
        Data.SetSubMissionStatus(missionId, MissionPhaseEnum.Finish); // set finish
        MarkMissionDataDirty();

        await SetMissionProgress(missionId, subMission.SubMissionInfo?.Progress ?? 1);

        var sync = new MissionSync();
        sync.MissionList.Add(new Proto.Mission
        {
            Id = (uint)missionId,
            Status = MissionStatus.MissionFinish,
            Progress = (uint)(subMission.SubMissionInfo?.Progress ?? 1)
        });

        var acceptedNextMission = false;

        // get next sub mission
        foreach (var nextMission in mainMission.MissionInfo?.SubMissionList ?? [])
        {
            if (nextMission.TakeType != SubMissionTakeTypeEnum.AnySequence &&
                nextMission.TakeType != SubMissionTakeTypeEnum.MultiSequence) continue;
            var canAccept = nextMission.TakeType == SubMissionTakeTypeEnum.MultiSequence; // mean and operation
            foreach (var id in nextMission.TakeParamIntList ?? [])
                if (GetSubMissionStatus(id) != MissionPhaseEnum.Finish &&
                    nextMission.TakeType == SubMissionTakeTypeEnum.MultiSequence)
                {
                    canAccept = false;
                    break;
                }
                else if (GetSubMissionStatus(id) == MissionPhaseEnum.Finish &&
                         nextMission.TakeType == SubMissionTakeTypeEnum.AnySequence) // or operation
                {
                    canAccept = true;
                    break;
                }

            if (canAccept)
            {
                var s = await AcceptSubMission(nextMission.ID, false, false);
                if (s != null)
                {
                    acceptedNextMission = true;
                    sync.MissionList.Add(new Proto.Mission
                    {
                        Id = (uint)nextMission.ID,
                        Status = MissionStatus.MissionDoing,
                        Progress = (uint)GetMissionProgress(nextMission.ID)
                    });
                }
            }
        }

        // 4.x mission resources often only provide an ordered sub-mission list via
        // partial layout without legacy take-type metadata. In that case, fall back
        // to a simple sequential chain so early main-story missions remain playable.
        if (!acceptedNextMission)
        {
            var sequentialNextMissionId = GetSequentialNextSubMissionId(mainMission, missionId);
            if (sequentialNextMissionId > 0)
            {
                var s = await AcceptSubMission(sequentialNextMissionId, false, false);
                if (s != null)
                    sync.MissionList.Add(new Proto.Mission
                    {
                        Id = (uint)sequentialNextMissionId,
                        Status = MissionStatus.MissionDoing,
                        Progress = (uint)GetMissionProgress(sequentialNextMissionId)
                    });
            }
        }

        await Player.SendPacket(new PacketPlayerSyncScNotify(sync));
        await Player.SendPacket(new PacketStartFinishSubMissionScNotify(missionId));
        await Player.SendPacket(new PacketFinishedMissionScNotify(missionId));

        if (mainMission.MissionInfo != null)
            await HandleFinishAction(mainMission.MissionInfo, missionId);

        // Get if it should finish main mission
        // get current main mission
        var shouldFinish = true;
        mainMission.MissionInfo?.FinishSubMissionList.ForEach(id =>
        {
            if (GetSubMissionStatus(id) != MissionPhaseEnum.Finish) shouldFinish = false;
        });

        foreach (var nextMission in GetRunningSubMissionList())
        {
            FinishTypeHandlers.TryGetValue(nextMission.FinishType, out var handler);
            if (handler != null) await handler.HandleMissionFinishType(Player, nextMission, null);
        }

        if (shouldFinish) await FinishMainMission(mainMissionId);

        if (missionId == 101140201)
        {
            // Player.ChangeAvatarPathType(8001, Enums.Avatar.MultiPathAvatarTypeEnum.Knight);
            var list = Player.LineupManager!.GetCurLineup()!.BaseAvatars!
                .Select(x => x.SpecialAvatarId > 0 ? x.SpecialAvatarId : x.BaseAvatarId).ToList();
            list[list.IndexOf(8001)] = Player.Data.CurrentGender == Gender.Man ? 1008003 : 1008004;
            Player.LineupManager!.SetExtraLineup(ExtraLineupType.LineupHeliobus, list);
        }

        if (missionId == 103040103)
            await Player.SendPacket(new PacketHeartDialScriptChangeScNotify(HeartDialUnlockStatus.UnlockSingle));

        if (missionId == 103040104)
            await Player.SendPacket(new PacketHeartDialScriptChangeScNotify(HeartDialUnlockStatus.UnlockAll));

        // handle reward
        await HandleSubMissionReward(missionId);
        //Player.StoryLineManager!.CheckIfEnterStoryLine();
        //Player.StoryLineManager!.CheckIfFinishStoryLine();

        // 4.x client task UI does not always rebuild from MissionSync alone after a sub-mission
        // switch. Push the lightweight full mission snapshot so the newly accepted sub-mission
        // appears in the task panel immediately.
        if (!shouldFinish)
            await RefreshMissionState(syncScene: false, syncStoryLine: false, syncQuest: false);

        PluginEvent.InvokeOnPlayerFinishSubMission(Player, missionId);
    }

    public async ValueTask<bool> ForceFinishSubMission(int missionId)
    {
        if (!ConfigManager.Config.ServerOption.EnableMission) return false;
        GameData.SubMissionInfoData.TryGetValue(missionId, out var subMission);
        if (subMission == null) return false;

        var status = Data.GetSubMissionStatus(missionId);
        if (status == MissionPhaseEnum.Finish) return false;

        if (Data.GetMainMissionStatus(subMission.MainMissionId) == MissionPhaseEnum.None)
            Data.SetMainMissionStatus(subMission.MainMissionId, MissionPhaseEnum.Accept);

        Data.SetSubMissionStatus(missionId, MissionPhaseEnum.Finish);
        Data.SubMissionProgressDict[missionId] = subMission.SubMissionInfo?.Progress ?? 1;
        if (Data.TrackingMainMissionId == 0)
            Data.TrackingMainMissionId = subMission.MainMissionId;
        MarkMissionDataDirty();

        GameData.MainMissionData.TryGetValue(subMission.MainMissionId, out var mainMission);
        if (mainMission != null)
        {
            var acceptedNextMission = false;

            foreach (var nextMission in mainMission.MissionInfo?.SubMissionList ?? [])
            {
                if (Data.GetSubMissionStatus(nextMission.ID) != MissionPhaseEnum.None) continue;
                if (nextMission.TakeType != SubMissionTakeTypeEnum.AnySequence &&
                    nextMission.TakeType != SubMissionTakeTypeEnum.MultiSequence) continue;

                var canAccept = nextMission.TakeType == SubMissionTakeTypeEnum.MultiSequence;
                foreach (var id in nextMission.TakeParamIntList ?? [])
                    if (GetSubMissionStatus(id) != MissionPhaseEnum.Finish &&
                        nextMission.TakeType == SubMissionTakeTypeEnum.MultiSequence)
                    {
                        canAccept = false;
                        break;
                    }
                    else if (GetSubMissionStatus(id) == MissionPhaseEnum.Finish &&
                             nextMission.TakeType == SubMissionTakeTypeEnum.AnySequence)
                    {
                        canAccept = true;
                        break;
                    }

                if (!canAccept) continue;

                Data.SetSubMissionStatus(nextMission.ID, MissionPhaseEnum.Accept);
                acceptedNextMission = true;
            }

            if (!acceptedNextMission)
            {
                var sequentialNextMissionId = GetSequentialNextSubMissionId(mainMission, missionId);
                if (sequentialNextMissionId > 0 && Data.GetSubMissionStatus(sequentialNextMissionId) == MissionPhaseEnum.None)
                    Data.SetSubMissionStatus(sequentialNextMissionId, MissionPhaseEnum.Accept);
            }

            var shouldFinishMain = mainMission.MissionInfo?.FinishSubMissionList.All(id =>
                GetSubMissionStatus(id) == MissionPhaseEnum.Finish) ?? false;

            if (shouldFinishMain)
                return await ForceFinishMainMission(subMission.MainMissionId);
        }

        MarkMissionDataDirty();
        await AcceptMainMissionByCondition(false);
        await RefreshMissionState(syncScene: false, syncStoryLine: false, syncQuest: false);
        return Data.GetSubMissionStatus(missionId) == MissionPhaseEnum.Finish;
    }

    private int GetSequentialNextSubMissionId(MainMissionExcel mainMission, int currentMissionId)
    {
        var orderedSubMissionIds = mainMission.MissionInfo?.SubMissionList
            .Select(x => x.ID)
            .Where(x => x > 0)
            .Distinct()
            .ToList() ?? [];

        var currentIndex = orderedSubMissionIds.IndexOf(currentMissionId);
        if (currentIndex < 0) return 0;

        for (var i = currentIndex + 1; i < orderedSubMissionIds.Count; i++)
        {
            var nextMissionId = orderedSubMissionIds[i];
            if (Data.GetSubMissionStatus(nextMissionId) == MissionPhaseEnum.None)
                return nextMissionId;
        }

        return 0;
    }

    public async ValueTask HandleFinishAction(MissionInfo info, int subMissionId)
    {
        var subMission = info.SubMissionList.Find(x => x.ID == subMissionId);
        if (subMission == null) return;

        foreach (var action in subMission.FinishActionList ?? []) await HandleFinishAction(action);
    }

    public async ValueTask HandleFinishAction(FinishActionInfo actionInfo)
    {
        ActionHandlers.TryGetValue(actionInfo.FinishActionType, out var handler);
        if (handler != null)
            await handler.OnHandle(actionInfo.FinishActionPara, actionInfo.FinishActionParaString, Player);
    }

    public async ValueTask HandleMissionReward(int mainMissionId)
    {
        GameData.MainMissionData.TryGetValue(mainMissionId, out var mainMission);
        if (mainMission == null) return;
        GameData.RewardDataData.TryGetValue(mainMission.RewardID, out var reward);
        var itemList = new List<ItemData>();

        foreach (var item in reward?.GetItems() ?? [])
        {
            GameData.ItemConfigData.TryGetValue(item.Item1, out var itemExcel);
            var res = await Player.InventoryManager!.AddItem(item.Item1, item.Item2,
                itemExcel?.ItemMainType == ItemMainTypeEnum.AvatarCard); // notify if avatar card
            if (res != null) itemList.Add(res);
        }

        var hCoin = await Player.InventoryManager!.AddItem(1, reward?.Hcoin ?? 0, false);
        if (hCoin != null) itemList.Add(hCoin);

        foreach (var i in mainMission.SubRewardList)
        {
            GameData.RewardDataData.TryGetValue(i, out var rewardDataExcel);
            var hCoin2 = await Player.InventoryManager!.AddItem(1, rewardDataExcel?.Hcoin ?? 0, false); // hcoin
            if (hCoin2 != null) itemList.Add(hCoin2);
            foreach (var item in rewardDataExcel?.GetItems() ?? []) // items
            {
                GameData.ItemConfigData.TryGetValue(item.Item1, out var itemExcel);
                var res = await Player.InventoryManager!.AddItem(item.Item1, item.Item2,
                    itemExcel?.ItemMainType == ItemMainTypeEnum.AvatarCard); // notify if avatar card
                if (res != null) itemList.Add(res);
            }
        }


        if (itemList.Count > 0)
            await Player.SendPacket(new PacketMissionRewardScNotify(mainMissionId, 0, itemList));
    }

    public async ValueTask HandleSubMissionReward(int subMissionId)
    {
        GameData.SubMissionInfoData.TryGetValue(subMissionId, out var subMission);
        if (subMission == null) return;
        GameData.RewardDataData.TryGetValue(subMission.SubMissionInfo?.SubRewardID ?? 0, out var reward);
        var itemList = new List<ItemData>();

        foreach (var item in reward?.GetItems() ?? [])
        {
            GameData.ItemConfigData.TryGetValue(item.Item1, out var itemExcel);
            var res = await Player.InventoryManager!.AddItem(item.Item1, item.Item2,
                itemExcel?.ItemMainType == ItemMainTypeEnum.AvatarCard); // notify if avatar card
            if (res != null) itemList.Add(res);
        }

        await Player.SendPacket(new PacketSubMissionRewardScNotify(subMissionId, itemList));
    }

    public async ValueTask HandleFinishType(MissionFinishTypeEnum finishType, object? arg = null, bool pushQuest = true)
    {
        FinishTypeHandlers.TryGetValue(finishType, out var handler);
        foreach (var mission in GetRunningSubMissionList())
            if (mission.FinishType == finishType)
                if (handler != null)
                    await handler.HandleMissionFinishType(Player, mission, arg);

        foreach (var quest in Player.QuestManager?.GetRunningQuest() ?? [])
        {
            var excel = GameData.QuestDataData.GetValueOrDefault(quest.QuestId);
            if (excel == null) continue;
            var finishWay = GameData.FinishWayData.GetValueOrDefault(excel.FinishWayID);
            if (finishWay == null) continue;
            if (finishWay.FinishType == finishType)
                if (handler != null)
                    await handler.HandleQuestFinishType(Player, excel, finishWay, arg);
        }

        if (pushQuest)
            await Player.QuestManager!.SyncQuest();
    }

    public async ValueTask HandleAllFinishType(object? arg = null)
    {
        foreach (var handler in FinishTypeHandlers) await HandleFinishType(handler.Key, arg, false);
        await Player.QuestManager!.SyncQuest();
    }

    public async ValueTask HandleTalkStr(string talkString)
    {
        if (!ConfigManager.Config.ServerOption.EnableMission) return;

        foreach (var mission in GetRunningSubMissionList())
            if (mission.FinishType == MissionFinishTypeEnum.Talk)
                if (mission.ParamStr1 == talkString)
                    await FinishSubMission(mission.ID);

        foreach (var quest in Player.QuestManager?.GetRunningQuest() ?? [])
        {
            var excel = GameData.QuestDataData.GetValueOrDefault(quest.QuestId);
            if (excel == null) continue;
            var finishWay = GameData.FinishWayData.GetValueOrDefault(excel.FinishWayID);
            if (finishWay == null) continue;
            if (finishWay.FinishType == MissionFinishTypeEnum.Talk)
                if (finishWay.ParamStr1 == talkString)
                    await Player.QuestManager!.FinishQuest(quest.QuestId);
        }
    }

    public async ValueTask HandleCustomValue(List<MissionCustomValue> values, int missionId)
    {
        if (!ConfigManager.Config.ServerOption.EnableMission) return;

        GameData.SubMissionInfoData.TryGetValue(missionId, out var subMission);
        if (subMission == null) return;
        var mainMissionId = subMission.MainMissionId;
        GameData.MainMissionData.TryGetValue(mainMissionId, out var mainMission);
        if (mainMission == null) return;

        foreach (var mission in mainMission.MissionInfo?.SubMissionList ?? [])
            if (mission.TakeType == SubMissionTakeTypeEnum.CustomValue)
            {
                var index = 0;
                var accept = false;
                List<List<int>> list = [mission.TakeParamIntList ?? []];
                if (mission.TakeParamIntList?.Count > 5)
                {
                    // every 3 as group
                    var group = mission.TakeParamIntList.Count / 3;
                    list = [];
                    for (var i = 0; i < group; i++)
                    {
                        var customValue = mission.TakeParamIntList.GetRange(i * 3, 3);
                        list.Add(customValue);
                    }
                }

                foreach (var customValues in list)
                {
                    var thisAccept = true;
                    foreach (var customValue in customValues)
                    {
                        if (customValue == 0 && index == 0) continue; // skip 0
                        var valueInst = values.Find(x => x.Index == index);
                        if (valueInst == null) continue;
                        if (valueInst.CustomValue != customValue)
                        {
                            thisAccept = false;
                            break;
                        }

                        index++;
                    }

                    if (thisAccept) accept = true; // accept if any group is true
                }

                if (accept) await AcceptSubMission(mission.ID);
            }
    }

    public async ValueTask AddMissionProgress(int missionId, int progress)
    {
        if (!ConfigManager.Config.ServerOption.EnableMission) return;

        Data.SubMissionProgressDict.TryGetValue(missionId, out var currentProgress);
        Data.SubMissionProgressDict[missionId] = currentProgress + progress;
        MarkMissionDataDirty();
        GameData.SubMissionInfoData.TryGetValue(missionId, out var subMission);
        if (subMission == null) return;

        if (currentProgress + progress >= (subMission.SubMissionInfo?.Progress ?? 1)) return;

        var sync = new MissionSync();
        sync.MissionList.Add(new Proto.Mission
        {
            Id = (uint)missionId,
            Progress = (uint)(currentProgress + progress)
        });

        await Player.SendPacket(new PacketPlayerSyncScNotify(sync));
    }

    public async ValueTask SetMissionProgress(int missionId, int progress, bool sendPacket = true)
    {
        if (!ConfigManager.Config.ServerOption.EnableMission) return;

        Data.SubMissionProgressDict[missionId] = progress;
        MarkMissionDataDirty();
        GameData.SubMissionInfoData.TryGetValue(missionId, out var subMission);
        if (subMission == null) return;

        if (progress >= (subMission.SubMissionInfo?.Progress ?? 1)) return;

        var sync = new MissionSync();
        sync.MissionList.Add(new Proto.Mission
        {
            Id = (uint)missionId,
            Progress = (uint)progress
        });

        if (sendPacket)
            await Player.SendPacket(new PacketPlayerSyncScNotify(sync));
    }

    public List<MissionCustomValue> GetMainMissionCustomValueList(int missionId)
    {
        var result = new List<MissionCustomValue>();
        var storedValues = Data.MainMissionCustomValueDict.GetValueOrDefault(missionId) ?? [];
        var missionInfo = GameData.MainMissionData.GetValueOrDefault(missionId)?.MissionInfo;

        var indexToKey = new Dictionary<int, string>();
        foreach (var (key, value) in storedValues)
        {
            if (TryGetMissionCustomValueIndex(key, out var index))
                indexToKey[index] = key;

            result.Add(new MissionCustomValue
            {
                DLCLDIAOIMK = key,
                CustomValue = value
            });
        }

        foreach (var customValue in missionInfo?.MissionCustomValueList ?? [])
        {
            var proto = new MissionCustomValue
            {
                Index = (uint)customValue.Index
            };

            if (indexToKey.TryGetValue(customValue.Index, out var key))
                proto.DLCLDIAOIMK = key;

            if (customValue.ValidValueParamList.Count > 0)
                proto.CustomValue = storedValues.GetValueOrDefault(proto.DLCLDIAOIMK,
                    (uint)customValue.ValidValueParamList[0]);
            else if (proto.DLCLDIAOIMK.Length > 0)
                proto.CustomValue = storedValues.GetValueOrDefault(proto.DLCLDIAOIMK);

            result.Add(proto);
        }

        return result
            .GroupBy(x => (x.DLCLDIAOIMK, x.Index))
            .Select(g => g.First())
            .ToList();
    }

    public void SetMainMissionCustomValue(int missionId, string key, uint value)
    {
        if (!Data.MainMissionCustomValueDict.TryGetValue(missionId, out var customValues))
        {
            customValues = [];
            Data.MainMissionCustomValueDict[missionId] = customValues;
        }

        customValues[key] = value;
        MarkMissionDataDirty();
    }

    private static bool TryGetMissionCustomValueIndex(string key, out int index)
    {
        index = 0;
        var digits = new string(key.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
        return digits.Length > 0 && int.TryParse(digits, out index);
    }

    #endregion

    #region Mission Status

    public MissionStatusBySceneInfo BuildSceneMissionInfo(int floorId)
    {
        var info = new MissionStatusBySceneInfo();
        var targetSubIds = GameData.MainMissionData.Values
            .Where(x => x.MissionInfo != null)
            .SelectMany(x => x.MissionInfo.SubMissionList)
            .Where(x => x.LevelFloorID == floorId || (x.GroupIDList?.Count ?? 0) > 0)
            .Select(x => x.ID)
            .Distinct()
            .ToHashSet();

        if (GameData.GetFloorInfo(floorId) is { } floorInfo)
        {
            foreach (var relatedMissionId in floorInfo.Groups.Values.SelectMany(x => x.RelatedMissionId))
                targetSubIds.Add(relatedMissionId);
        }

        HashSet<int> mainIds = [];
        foreach (var mainMission in GameData.MainMissionData.Values)
        foreach (var subMission in mainMission.MissionInfo.SubMissionList)
            if (targetSubIds.Contains(subMission.ID))
            {
                info.SubMissionStatusList.Add(new Proto.Mission
                {
                    Id = (uint)subMission.ID,
                    Status = GetSubMissionStatus(subMission.ID).ToProto(),
                    Progress = (uint)GetMissionProgress(subMission.ID)
                });

                mainIds.Add(mainMission.MainMissionID);
            }

        foreach (var mainId in mainIds)
        {
            if (GetMainMissionStatus(mainId) == MissionPhaseEnum.Finish)
                info.FinishedMainMissionIdList.Add((uint)mainId);
            else if (GetMainMissionStatus(mainId) == MissionPhaseEnum.Accept)
                info.UnfinishedMainMissionIdList.Add((uint)mainId);

            var customValueList = GetMainMissionCustomValueList(mainId);
            if (customValueList.Count > 0)
                info.NEDJGJNLIPF.Add(new MainMissionCustomValue
                {
                    MainMissionId = (uint)mainId,
                    CustomValueList = new EPNCDFBAHEA
                    {
                        CustomValueList = { customValueList }
                    }
                });
        }

        var currentFloor = floorId;
        if (currentFloor != 0)
            foreach (var pkg in GameData.ContentPackageConfigData.Values)
            {
                if (pkg.MainMissionIDList.Count == 0) continue;
                if (!pkg.TouchesFloor(currentFloor)) continue;

                foreach (var missionId in pkg.MainMissionIDList)
                {
                    var mid = (uint)missionId;
                    if (info.FinishedMainMissionIdList.Contains(mid)) continue;
                    if (info.UnfinishedMainMissionIdList.Contains(mid)) continue;
                    info.UnfinishedMainMissionIdList.Add(mid);
                }
            }

        return info;
    }

    public MissionPhaseEnum GetMainMissionStatus(int missionId)
    {
        if (!ConfigManager.Config.ServerOption.EnableMission) return MissionPhaseEnum.Finish;

        return Data.GetMainMissionStatus(missionId);
    }

    public MissionPhaseEnum GetSubMissionStatus(int missionId)
    {
        if (!ConfigManager.Config.ServerOption.EnableMission) return MissionPhaseEnum.Finish;

        return Data.GetSubMissionStatus(missionId);
    }

    public SubMissionInfo? GetSubMissionInfo(int missionId)
    {
        GameData.SubMissionInfoData.TryGetValue(missionId, out var subMission);
        if (subMission == null) return null;
        return subMission.SubMissionInfo;
    }

    public List<int> GetRunningSubMissionIdList()
    {
        if (!ConfigManager.Config.ServerOption.EnableMission) return [];

        var list = new List<int>();
        list.AddRange(Data.RunningSubMissionIds);
        return list;
    }

    public List<SubMissionInfo> GetRunningSubMissionList()
    {
        if (!ConfigManager.Config.ServerOption.EnableMission) return [];

        var list = new List<SubMissionInfo>();
        var ids = new List<int>();
        ids.AddRange(Data.RunningSubMissionIds);
        foreach (var id in ids)
        {
            GameData.SubMissionInfoData.TryGetValue(id, out var mission);
            if (mission != null && mission.SubMissionInfo != null) list.Add(mission.SubMissionInfo);
        }

        return list;
    }

    public bool IsMainMissionVisible(int mainMissionId)
    {
        return GameData.MainMissionData.GetValueOrDefault(mainMissionId)?.IsShow != false;
    }

    public bool TryResolveMainMissionId(int missionId, out int mainMissionId)
    {
        mainMissionId = 0;

        if (GameData.MainMissionData.ContainsKey(missionId))
        {
            mainMissionId = missionId;
            return true;
        }

        if (GameData.SubMissionInfoData.TryGetValue(missionId, out var subMissionInfo))
        {
            mainMissionId = subMissionInfo.MainMissionId;
            return mainMissionId > 0;
        }

        if (GameData.SubMissionData.TryGetValue(missionId, out var subMissionData))
        {
            mainMissionId = subMissionData.MainMissionID;
            return mainMissionId > 0;
        }

        return false;
    }

    public bool HasMainMissionDefinition(int mainMissionId)
    {
        if (!GameData.MainMissionData.TryGetValue(mainMissionId, out var mainMission))
            return false;

        if (mainMission.SubMissionIds.Count > 0)
            return true;

        if (mainMission.MissionInfo == null)
            return false;

        return mainMission.MissionInfo.SubMissionList.Count > 0 ||
               mainMission.MissionInfo.StartSubMissionList.Count > 0 ||
               mainMission.MissionInfo.FinishSubMissionList.Count > 0;
    }

    public int GetClientTrackingMainMissionId()
    {
        if (Data.TrackingMainMissionId > 0 && IsMainMissionVisible(Data.TrackingMainMissionId))
            return Data.TrackingMainMissionId;

        foreach (var mainMissionId in Data.RunningMainMissionIds)
            if (IsMainMissionVisible(mainMissionId))
                return mainMissionId;

        return 0;
    }

    public List<SubMissionInfo> GetClientRunningSubMissionList()
    {
        return GetRunningSubMissionList()
            .Where(x => IsMainMissionVisible(x.MainMissionID))
            .ToList();
    }

    public int GetMissionProgress(int missionId)
    {
        GameData.SubMissionInfoData.TryGetValue(missionId, out var subMission);
        if (!ConfigManager.Config.ServerOption.EnableMission) return subMission?.SubMissionInfo?.Progress ?? 0;

        Data.SubMissionProgressDict.TryGetValue(missionId, out var progress);
        return progress;
    }

    #endregion

    #region Handlers

    public async ValueTask OnBattleFinish(BattleInstance instance, PVEBattleResultCsReq req)
    {
        foreach (var mission in GetRunningSubMissionIdList())
        {
            var subMission = GetSubMissionInfo(mission);
            if (subMission != null && subMission.FinishType == MissionFinishTypeEnum.StageWin &&
                req.EndStatus == BattleEndStatus.BattleEndWin) // TODO: Move to handler
                if (req.StageId.ToString().StartsWith(subMission.ParamInt1.ToString()))
                {
                    await FinishSubMission(mission);
                    instance.EventId = 0;
                }
        }

        await HandleAllFinishType(instance);
    }

    public async ValueTask OnPlayerInteractWithProp()
    {
        foreach (var id in GetRunningSubMissionIdList())
            if (GetSubMissionInfo(id)?.FinishType == MissionFinishTypeEnum.PropState)
            {
                FinishTypeHandlers.TryGetValue(MissionFinishTypeEnum.PropState, out var handler);
                if (handler != null) await handler.HandleMissionFinishType(Player, GetSubMissionInfo(id)!, null);
            }
    }

    public async ValueTask OnPlayerChangeScene()
    {
        foreach (var id in GetRunningSubMissionIdList())
        {
            var info = GetSubMissionInfo(id);
            if (info == null) continue;

            if (info.LevelFloorID == Player.SceneInstance?.FloorId)
            {
                if (info.GroupIDList == null) continue;
                foreach (var group in info.GroupIDList)
                    await Player.SceneInstance.EntityLoader!.LoadGroup(group, false);
            }
        }
    }

    public void OnLoadScene(SceneInfo info)
    {
        var floorId = Player.SceneInstance?.FloorId ?? (int)info.FloorId;
        info.SceneMissionInfo = BuildSceneMissionInfo(floorId);
    }

    #endregion
}
