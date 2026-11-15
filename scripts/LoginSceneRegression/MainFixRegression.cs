using Google.Protobuf;
using March7thHoney.Data;
using March7thHoney.Database;
using March7thHoney.Database.Generated;
using March7thHoney.Database.Lineup;
using March7thHoney.Database.Player;
using March7thHoney.Database.Scene;
using March7thHoney.Enums.Item;
using March7thHoney.Enums.Scene;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server.Packet;
using March7thHoney.GameServer.Server.Packet.Recv.Lineup;
using March7thHoney.GameServer.Server.Packet.Recv.PetMarble;
using March7thHoney.GameServer.Server.Packet.Recv.Player;
using March7thHoney.GameServer.Server.Packet.Send.Player;
using March7thHoney.GameServer.Server.Packet.Send.PlayerBoard;
using March7thHoney.GameServer.Server.Packet.Recv.Raid;
using March7thHoney.GameServer.Server.Packet.Recv.RelicPreset;
using March7thHoney.GameServer.Server.Packet.Recv.Rogue;
using March7thHoney.GameServer.Server.Packet.Recv.TrainCakeCatch;
using March7thHoney.GameServer.Server.Packet.Send.Avatar;
using March7thHoney.GameServer.Server.Packet.Send.Lineup;
using March7thHoney.Kcp;
using March7thHoney.Kcp.KcpSharp;
using March7thHoney.Proto;
using March7thHoney.Util;
using MemoryPack;
using Microsoft.Data.Sqlite;
using StoredLineup = March7thHoney.Database.Lineup.LineupInfo;

internal static class MainFixRegression
{
    public static async Task Run(string scratch, KcpConversation peer, Action<string, bool> check,
        Func<Func<Task>, Task<List<(ushort Id, byte[] Body)>>> capture)
    {
        var player = new PlayerInstance(60022);
        await player.InitializeAsync();
        var connection = new ProbeConnection(peer) { Player = player };
        player.Connection = connection;
        Task<List<(ushort Id, byte[] Body)>> Request(Handler handler, IMessage req) =>
            capture(() => handler.OnHandle(connection, [], req.ToByteArray()));
        T Response<T>(List<(ushort Id, byte[] Body)> frames, int id, MessageParser<T> parser) where T : IMessage<T> =>
            parser.ParseFrom(frames.Single(frame => frame.Id == id).Body);
        async Task<T> Stored<T>() where T : BaseDatabaseDataHelper
        {
            await using var db = new SqliteConnection($"Data Source={Path.Combine(scratch, "regression.db")}");
            await db.OpenAsync();
            return EntityStores.Map[typeof(T)].LoadAll(db).OfType<T>().Single(data => data.Uid == player.Uid);
        }

        // The 4.8 function-unlock arm is oneof field 15 (4.7 used 10, 4.6 used 1); 3 and 6 are the other two arms.
        var query = new CommonRogueQueryCsReq { AGFFNBFBHDL = { 3, 15, 6, 15, 999 } };
        var queryReply = Response(await Request(new HandlerCommonRogueQueryCsReq(), query),
            CmdIds.CommonRogueQueryScRsp, CommonRogueQueryScRsp.Parser);
        check("CommonRogueQuery uses the active unlock field 15 and preserves requested order and duplicates",
            CCKJAEDDDCL.BBEKBJPHEJKFieldNumber == 15 && CCKJAEDDDCL.MECOEFEJANEFieldNumber == 3 &&
            CCKJAEDDDCL.BMGOLFNGHGHFieldNumber == 6 && queryReply.LFLCDNGALME.Count == 5 &&
            queryReply.LFLCDNGALME[1].BBEKBJPHEJK?.LFCGPOKMJLC.Count == Enum.GetValues<MGJDNGHDIKG>().Length &&
            queryReply.LFLCDNGALME[3].BBEKBJPHEJK?.LFCGPOKMJLC.Count == Enum.GetValues<MGJDNGHDIKG>().Length &&
            new[] { 0, 2, 4 }.All(index => queryReply.LFLCDNGALME[index].KKNBOACNCONCase == CCKJAEDDDCL.KKNBOACNCONOneofCase.None));
        var emptyQuery = Response(await Request(new HandlerCommonRogueQueryCsReq(), new CommonRogueQueryCsReq()),
            CmdIds.CommonRogueQueryScRsp, CommonRogueQueryScRsp.Parser);
        check("empty CommonRogueQuery returns no invented branches", emptyQuery.LFLCDNGALME.Count == 0);

        var social = Response(await Request(new HandlerSocialPlayGetDataCsReq(), new SocialPlayGetDataCsReq()),
            CmdIds.SocialPlayGetDataScRsp, SocialPlayGetDataScRsp.Parser);
        var marble = Response(await Request(new HandlerPetMarbleGetDataCsReq(), new PetMarbleGetDataCsReq()),
            CmdIds.PetMarbleGetDataScRsp, PetMarbleGetDataScRsp.Parser);
        var cycle = Response(await Request(new HandlerPetMarbleGetCycleTrialPetsCsReq(), new PetMarbleGetCycleTrialPetsCsReq()),
            CmdIds.PetMarbleGetCycleTrialPetsScRsp, PetMarbleGetCycleTrialPetsScRsp.Parser);
        check("4.7 minigame data handlers return present nested data through real KCP requests",
            social.Retcode == 0 && social.ELDIONMAAHO != null && social.NDDJFLPBHBL != null && social.AKDPEJABLPI.Count == 0 &&
            marble.Retcode == 0 && marble.BJBJLBIOPKA != null && cycle.Retcode == 0 && cycle.ACCGIILHFKL != null);
        var branchless = ProtoPresence.FillEmptySubMessages(new CCKJAEDDDCL());
        check("empty-message presence helper leaves oneof choices unset",
            branchless.KKNBOACNCONCase == CCKJAEDDDCL.KKNBOACNCONOneofCase.None);
        HandlerManager.Init();
        check("new 4.7 handlers are registered by generated opcode dispatch",
            HandlerManager.GetHandler(CmdIds.MarkPresetLineupCsReq) is HandlerMarkPresetLineupCsReq &&
            HandlerManager.GetHandler(CmdIds.SocialPlayGetDataCsReq) is HandlerSocialPlayGetDataCsReq &&
            HandlerManager.GetHandler(CmdIds.PetMarbleGetDataCsReq) is HandlerPetMarbleGetDataCsReq &&
            HandlerManager.GetHandler(CmdIds.PetMarbleGetCycleTrialPetsCsReq) is HandlerPetMarbleGetCycleTrialPetsCsReq);

        // Legacy dictionary order need not equal team indexes; extra teams also contribute to Count.
        var data = player.LineupManager!.Data;
        var fifth = new StoredLineup { Name = "five", LeaderAvatarId = 1001, BaseAvatars = [new() { BaseAvatarId = 1001 }] };
        var extra = new StoredLineup { Name = "extra", LineupType = (int)ExtraLineupType.LineupChallenge, BaseAvatars = [] };
        data.Lineups.Clear();
        data.Lineups[11] = extra;
        data.Lineups[5] = fifth;
        data.Lineups[0] = new StoredLineup { Name = "", BaseAvatars = null };
        data.CurLineup = 3;
        data.CurExtraLineup = -1;
        player.Connection = null;
        player = new PlayerInstance(player.Data);
        await player.InitializeAsync();
        connection = new ProbeConnection(peer) { Player = player };
        player.Connection = connection;
        var all = GetAllLineupDataScRsp.Parser.ParseFrom(new PacketGetAllLineupDataScRsp(player).Data);
        check("legacy team gaps are padded by id and normal teams precede extra teams",
            Enumerable.Range(0, 9).All(data.Lineups.ContainsKey) && all.LineupList.Count == 10 &&
            all.LineupList.Take(9).Select(team => team.Index).SequenceEqual(Enumerable.Range(0, 9).Select(index => (uint)index)) &&
            all.LineupList[5].Name == "five" && all.LineupList[9].ExtraLineupType == ExtraLineupType.LineupChallenge &&
            data.Lineups[0].BaseAvatars != null);
        check("login repairs current empty normal team using the first playable saved team",
            data.CurLineup == 5 && ReferenceEquals(player.LineupManager!.GetCurLineup(), fifth));
        check("padding teams again preserves populated and virtual teams without creating duplicates",
            player.LineupManager!.GetAllLineup().Count == 10 && ReferenceEquals(data.Lineups[5], fifth) && ReferenceEquals(data.Lineups[11], extra));
        data.CurExtraLineup = 11;
        player.LineupManager.EnsurePlayableCurLineup();
        check("normal team fallback leaves an active virtual team selected", data.CurExtraLineup == 11);
        data.CurExtraLineup = -1;
        DatabaseHelper.Instance!.SaveDatabase();
        var favourite = Response(await Request(new HandlerMarkPresetLineupCsReq(), new MarkPresetLineupCsReq { Index = 5, IsFavourite = true }),
            CmdIds.SetTeamFavourite, SetTeamFavourite.Parser);
        check("favourite team answers the active commands 779 to 721 and marks the correct team",
            CmdIds.MarkPresetLineupCsReq == 779 && CmdIds.SetTeamFavourite == 721 && favourite.Retcode == 0 &&
            favourite.Index == 5 && favourite.IsFavourite && fifth.ToProto().IsFavourite);
        DatabaseHelper.Instance.SaveDatabase();
        check("favourite flag and repaired team indexes survive SQLite storage", (await Stored<LineupData>()).Lineups[5].IsFavourite &&
            (await Stored<LineupData>()).CurLineup == 5);
        var invalidFavourite = Response(await Request(new HandlerMarkPresetLineupCsReq(), new MarkPresetLineupCsReq { Index = uint.MaxValue, IsFavourite = true }),
            CmdIds.SetTeamFavourite, SetTeamFavourite.Parser);
        check("invalid favourite request cannot alter a real or virtual team", invalidFavourite.Retcode == (uint)Retcode.RetLineupNotExist &&
            fifth.IsFavourite && !extra.IsFavourite);
        await Request(new HandlerMarkPresetLineupCsReq(), new MarkPresetLineupCsReq { Index = 5, IsFavourite = false });
        DatabaseHelper.Instance.SaveDatabase();
        check("removing favourite persists without changing the team roster", !(await Stored<LineupData>()).Lineups[5].IsFavourite &&
            fifth.BaseAvatars!.Single().BaseAvatarId == 1001);
        var oldBytes = MemoryPackSerializer.Serialize(new PreFavouriteLineup { Name = "old", Mp = 3, BaseAvatars = [new() { BaseAvatarId = 1001 }] });
        var old = MemoryPackSerializer.Deserialize<StoredLineup>(oldBytes);
        check("pre-favourite MemoryPack contract loads with a default false flag",
            oldBytes[0] == 5 && old is { Name: "old", Mp: 3, IsFavourite: false } && old.BaseAvatars!.Single().BaseAvatarId == 1001);

        var hero = player.AvatarManager!.GetHero()!;
        player.Data.CurrentGender = Gender.Woman;
        player.Data.IsGenderSet = true;
        hero.AvatarId = 8001;
        check("hero login normalization corrects mismatched gender and is idempotent",
            player.AvatarManager.NormalizeHeroCurPath() && hero.AvatarId == 8002 && player.Data.CurBasicType == 8002 &&
            !player.AvatarManager.NormalizeHeroCurPath());
        hero.AvatarId = 1001;
        player.AvatarManager.NormalizeHeroCurPath();
        var avatarReply = GetAvatarDataScRsp.Parser.ParseFrom(new PacketGetAvatarDataScRsp(player).Data);
        check("invalid hero current id falls back to an announced owned path",
            hero.AvatarId == 8002 && avatarReply.AvatarList.Single(avatar => avatar.BaseAvatarId == 8001).CurMultiPathAvatarType == 8002 &&
            avatarReply.AvatarPathDataInfoList.Any(path => path.AvatarId == 8002));
        player.Data.CurBasicType = 8001;
        check("hero repair also reconciles a stale player basic type with the existing path",
            player.AvatarManager.NormalizeHeroCurPath() && hero.AvatarId == 8002 && player.Data.CurBasicType == 8002);
        hero.PathInfos.Remove(8002);
        check("missing current hero path is recreated before client serialization",
            player.AvatarManager.NormalizeHeroCurPath() && hero.PathInfos.ContainsKey(8002));
        player.MissionManager!.Data.EnableMissionOverride = true;
        player.Data.IsGenderSet = false;
        hero.AvatarId = 8001;
        player.Data.CurBasicType = 8001;
        check("unselected story gender is not forced to the saved default gender",
            !player.AvatarManager.NormalizeHeroCurPath() && hero.AvatarId == 8001);
        player.MissionManager.Data.EnableMissionOverride = false;
        player.Data.IsGenderSet = true;
        player.AvatarManager.NormalizeHeroCurPath();

        var relicExcel = GameData.RelicConfigData.Values.First(relic => GameData.ItemConfigData.ContainsKey(relic.ID) &&
            GameData.RelicMainAffixData.ContainsKey(relic.MainAffixGroup) && GameData.RelicSubAffixData.ContainsKey(relic.SubAffixGroup));
        var relic = (await player.InventoryManager!.AddItem(relicExcel.ID, 1, false, sync: false, returnRaw: true))!;
        DatabaseHelper.MarkDirty(player.Uid);
        DatabaseHelper.Instance.SaveDatabase();
        var addReq = new RelicPresetAddPlanCsReq { AvatarId = 1001, Name = "owned", RelicIds = { (uint)relic.UniqueId, 0 } };
        var added = Response(await Request(new HandlerRelicPresetAddPlanCsReq(), addReq), CmdIds.RelicPresetAddPlanScRsp, RelicPresetAddPlanScRsp.Parser);
        var planId = added.RelicPlan?.UniqueId ?? 0;
        check("relic preset accepts owned relics and empty slots using the 4.7 handler",
            added.Retcode == 0 && planId > 0 && added.RelicPlan!.RelicIds.SequenceEqual(addReq.RelicIds));
        DatabaseHelper.Instance.SaveDatabase();
        check("valid relic preset creation alone marks data for SQLite persistence",
            (await Stored<March7thHoney.Database.Inventory.InventoryData>()).RelicPresetPlans[(int)planId].Name == "owned");
        foreach (var (name, avatarId, ids) in new (string, uint, uint[])[]
        {
            ("unowned avatar", 999999, [(uint)relic.UniqueId]),
            ("empty list", 1001, []),
            ("unowned relic", 1001, [uint.MaxValue]),
            ("mixed valid and invalid relics", 1001, [(uint)relic.UniqueId, uint.MaxValue])
        })
        {
            var count = player.InventoryManager.Data.RelicPresetPlans.Count;
            var request = new RelicPresetAddPlanCsReq { AvatarId = avatarId, Name = "invalid" };
            request.RelicIds.AddRange(ids);
            var rejected = Response(await Request(new HandlerRelicPresetAddPlanCsReq(), request), CmdIds.RelicPresetAddPlanScRsp, RelicPresetAddPlanScRsp.Parser);
            check("relic preset rejects " + name + " without creating state", rejected.Retcode == (uint)Retcode.RetReqParaInvalid &&
                player.InventoryManager.Data.RelicPresetPlans.Count == count);
        }
        foreach (var req in new[]
        {
            new RelicPresetUpdatePlanCsReq { UniqueId = planId },
            new RelicPresetUpdatePlanCsReq { UniqueId = planId, RelicList = new HPPOINFLEPJ() },
            new RelicPresetUpdatePlanCsReq { UniqueId = planId, RelicList = new HPPOINFLEPJ { RelicIds = { uint.MaxValue } } }
        })
        {
            var rejected = Response(await Request(new HandlerRelicPresetUpdatePlanCsReq(), req), CmdIds.RelicPresetUpdatePlanScRsp, RelicPresetUpdatePlanScRsp.Parser);
            check("invalid relic preset update preserves the stored loadout", rejected.Retcode != 0 &&
                player.InventoryManager.Data.RelicPresetPlans[(int)planId].Name == "owned" &&
                player.InventoryManager.Data.RelicPresetPlans[(int)planId].RelicIds.SequenceEqual(new[] { relic.UniqueId, 0 }));
        }
        await Request(new HandlerRelicPresetUpdatePlanCsReq(), new RelicPresetUpdatePlanCsReq { UniqueId = planId, Name = "renamed" });
        DatabaseHelper.Instance.SaveDatabase();
        check("relic preset rename alone persists with unchanged relic references",
            (await Stored<March7thHoney.Database.Inventory.InventoryData>()).RelicPresetPlans[(int)planId].Name == "renamed");
        await Request(new HandlerRelicPresetDeletePlanCsReq(), new RelicPresetDeletePlanCsReq { ONIAFLNBNPN = planId });
        DatabaseHelper.Instance.SaveDatabase();
        check("relic preset deletion alone persists", !(await Stored<March7thHoney.Database.Inventory.InventoryData>()).RelicPresetPlans.ContainsKey((int)planId));

        // Missing config/record and stale return entrance must all release the player.
        var raid = player.RaidManager!.Data;
        const int absentRaid = 999999;
        async Task SeedOrphan(int oldEntry, RaidStatus status = RaidStatus.Doing)
        {
            await capture(async () => await player.TryEnterScene(101010102, 0, false));
            raid.CurRaidId = absentRaid;
            raid.CurRaidWorldLevel = 0;
            raid.RaidRecordDatas[absentRaid] = new() { [0] = new RaidRecord
            {
                RaidId = absentRaid, Status = status, OldEntryId = oldEntry,
                OldPos = new(111, 222, 333), OldRot = new(0, 90000, 0)
            } };
            player.LineupManager!.SetExtraLineup(ExtraLineupType.LineupHeliobus, [1001]);
        }
        await SeedOrphan(101010102);
        var leaveFrames = await Request(new HandlerLeaveRaidCsReq(), new LeaveRaidCsReq { RaidId = absentRaid, IsSave = true });
        check("leaving a config-less raid returns to saved entrance and releases its virtual team",
            raid.CurRaidId == 0 && raid.CurRaidWorldLevel == 0 && !raid.RaidRecordDatas.ContainsKey(absentRaid) &&
            player.Data.EntryId == 101010102 && player.Data.Pos!.X == 111 && data.CurExtraLineup == -1 &&
            leaveFrames.Any(frame => frame.Id == CmdIds.LeaveRaidScRsp));
        var repeatLeave = await Request(new HandlerLeaveRaidCsReq(), new LeaveRaidCsReq { RaidId = absentRaid });
        check("repeated leave request is ACK-only after raid release", repeatLeave.Select(frame => frame.Id)
            .SequenceEqual(new[] { (ushort)CmdIds.LeaveRaidScRsp }));
        await SeedOrphan(0, RaidStatus.Finish);
        await Request(new HandlerLeaveRaidCsReq(), new LeaveRaidCsReq { RaidId = absentRaid, IsSave = true });
        check("finished orphan raid with invalid return point falls back to default spawn",
            raid.CurRaidId == 0 && !raid.RaidRecordDatas.ContainsKey(absentRaid) && player.Data.EntryId == PlayerInstance.DefaultSpawnEntryId);
        raid.CurRaidId = absentRaid;
        raid.CurRaidWorldLevel = 4;
        await Request(new HandlerLeaveRaidCsReq(), new LeaveRaidCsReq { RaidId = absentRaid, IsSave = true });
        check("missing raid record cannot trap the leave request", raid.CurRaidId == 0 && raid.CurRaidWorldLevel == 0 &&
            player.Data.EntryId == PlayerInstance.DefaultSpawnEntryId);
        await SeedOrphan(101010102);
        await capture(async () => await player.RaidManager.OnLogin());
        check("login abandons a raid removed from resource tables before the saved scene loads",
            raid.CurRaidId == 0 && !raid.RaidRecordDatas.ContainsKey(absentRaid) && data.CurExtraLineup == -1 &&
            player.Data.EntryId == 101010102 && player.Data.Pos!.X == 111);
        var raidEntrance = GameData.MapEntranceData.Values.First(entrance =>
            GameData.MazePlaneData.GetValueOrDefault(entrance.PlaneID)?.PlaneType == PlaneTypeEnum.Raid);
        await SeedOrphan(raidEntrance.ID);
        await Request(new HandlerLeaveRaidCsReq(), new LeaveRaidCsReq { RaidId = absentRaid, IsSave = true });
        check("a stale raid return entrance cannot re-enter a run after release", raid.CurRaidId == 0 &&
            player.Data.EntryId == PlayerInstance.DefaultSpawnEntryId);
        player.Data.PlaneId = raidEntrance.PlaneID;
        player.Data.FloorId = raidEntrance.FloorID;
        player.Data.EntryId = raidEntrance.ID;
        raid.CurRaidId = absentRaid;
        await capture(async () => await player.OnLogin());
        check("login with a lost raid record loads a playable fallback scene", raid.CurRaidId == 0 &&
            player.SceneInstance?.EntryId == PlayerInstance.DefaultSpawnEntryId);
        player.Data.PlaneId = raidEntrance.PlaneID;
        player.Data.FloorId = raidEntrance.FloorID;
        player.Data.EntryId = raidEntrance.ID;
        player.StoryLineManager!.StoryLineData.CurStoryLineId = 1;
        await capture(async () => await player.RaidManager.OnLogin());
        check("raid login recovery leaves a story-owned location intact", player.Data.EntryId == raidEntrance.ID);
        player.StoryLineManager.StoryLineData.CurStoryLineId = 0;
        var validConfig = GameData.RaidConfigData.Values.First();
        raid.CurRaidId = validConfig.RaidID;
        raid.CurRaidWorldLevel = validConfig.HardLevel;
        raid.RaidRecordDatas[validConfig.RaidID] = new() { [validConfig.HardLevel] = new RaidRecord
            { RaidId = validConfig.RaidID, WorldLevel = validConfig.HardLevel, Status = RaidStatus.Doing,
                OldEntryId = 101010102, OldPos = new(111, 222, 333), OldRot = new() } };
        var validResume = await capture(async () => await player.RaidManager.OnLogin());
        check("valid raid remains resumable and announces its existing record", raid.CurRaidId == validConfig.RaidID &&
            validResume.Any(frame => frame.Id == CmdIds.RaidInfoNotify && RaidInfoNotify.Parser.ParseFrom(frame.Body).RaidId == validConfig.RaidID));
        await Request(new HandlerLeaveRaidCsReq(), new LeaveRaidCsReq { RaidId = (uint)validConfig.RaidID, IsSave = true });
        check("leaving a valid raid with save retains its resumable record and returns outside",
            raid.CurRaidId == 0 && raid.RaidRecordDatas[validConfig.RaidID].ContainsKey(validConfig.HardLevel) &&
            raid.RaidRecordDatas[validConfig.RaidID][validConfig.HardLevel].Status == RaidStatus.Doing &&
            player.Data.EntryId == 101010102);
        check("reset raid notify carries an empty item list", RaidInfoNotify.Parser.ParseFrom(
            new March7thHoney.GameServer.Server.Packet.Send.Raid.PacketRaidInfoNotify().Data).ItemList != null);
        player.Connection = null;

        await HeadFrameDisplay(scratch, peer, check, capture);
    }

    /// <summary>
    ///     Head-frame display switch. UpdatePlayerSetting oneof 1600 and PlayerSettingInfo field 11 both mean "hidden"
    ///     (the client sets IsDisplayHeadFrame to the negated value), and the client only offers the switch when a
    ///     HeadIconFrame item is in the bag. Hiding blanks the frame for other players, not on the player's own board.
    /// </summary>
    private static async Task HeadFrameDisplay(string scratch, KcpConversation peer, Action<string, bool> check,
        Func<Func<Task>, Task<List<(ushort Id, byte[] Body)>>> capture)
    {
        var frames = GameData.ItemConfigData.Values.Where(item => item.ItemSubType == ItemSubTypeEnum.HeadIconFrame)
            .Select(item => (uint)item.ID).Order().Take(2).ToList();
        check("cache carries at least two head-frame items", frames.Count == 2);
        if (frames.Count < 2) return;

        var player = new PlayerInstance(60028);
        await player.InitializeAsync();
        var connection = new ProbeConnection(peer) { Player = player };
        player.Connection = connection;
        var bag = player.InventoryManager!;
        int Owned(uint id) => bag.GetItem((int)id)?.Count ?? 0;
        uint SeenByOthers() => player.Data.ToSimpleProto(FriendOnlineStatus.Online).HeadFrameInfo?.HeadFrameItemId ?? 0;
        uint SeenInDetail() => player.Data.ToDetailProto().HeadFrameInfo?.HeadFrameItemId ?? 0;
        uint OwnBoard() => GetPlayerBoardDataScRsp.Parser.ParseFrom(new PacketGetPlayerBoardDataScRsp(player).Data)
            .HeadFrameInfo?.HeadFrameItemId ?? 0;
        bool HiddenAtLogin() => GetBasicInfoScRsp.Parser.ParseFrom(new PacketGetBasicInfoScRsp(player).Data)
            .PlayerSettingInfo?.BOMGNMDOGLM == true;
        async Task<UpdatePlayerSettingScRsp> Setting(UpdatePlayerSetting setting)
        {
            var sent = await capture(() => new HandlerUpdatePlayerSettingCsReq().OnHandle(connection, [],
                new UpdatePlayerSettingCsReq { PlayerSetting = setting }.ToByteArray()));
            return UpdatePlayerSettingScRsp.Parser.ParseFrom(
                sent.Single(frame => frame.Id == CmdIds.UpdatePlayerSettingScRsp).Body);
        }

        var granted = await capture(async () => await HeadFrameService.Grant(player, frames[0], long.MaxValue));
        check("granting a head frame puts one item in the bag and syncs it to the client",
            Owned(frames[0]) == 1 && granted.Any(frame => frame.Id == CmdIds.PlayerSyncScNotify &&
                PlayerSyncScNotify.Parser.ParseFrom(frame.Body).MaterialList.Any(item => item.Tid == frames[0])));
        await capture(async () => await HeadFrameService.Grant(player, frames[0], long.MaxValue));
        check("granting the same head frame again does not add a second item", Owned(frames[0]) == 1);
        check("a head frame is shown by default: login setting, other players and the own board",
            !player.Data.PrivacySettings.HideHeadFrame && !HiddenAtLogin() && SeenByOthers() == frames[0] &&
            SeenInDetail() == frames[0] && OwnBoard() == frames[0]);

        var hide = await Setting(new UpdatePlayerSetting { BOMGNMDOGLM = true });
        check("hiding the head frame echoes oneof 1600 and is sent back at login",
            hide.PlayerSetting?.BKAGLMFNKKACase == UpdatePlayerSetting.BKAGLMFNKKAOneofCase.BOMGNMDOGLM &&
            hide.PlayerSetting.BOMGNMDOGLM && player.Data.PrivacySettings.HideHeadFrame && HiddenAtLogin());
        check("a hidden head frame is blanked for other players but stays worn and on the own board",
            SeenByOthers() == 0 && SeenInDetail() == 0 && OwnBoard() == frames[0] &&
            player.Data.HeadFrame.HeadFrameId == frames[0] && Owned(frames[0]) == 1);
        await Setting(new UpdatePlayerSetting { NJAKOMDMFJD = true });
        check("another player setting leaves the head-frame switch alone", player.Data.PrivacySettings.HideHeadFrame);
        await capture(async () => await HeadFrameService.Grant(player, frames[1], long.MaxValue));
        check("a head frame granted while hidden is worn but stays hidden",
            player.Data.PrivacySettings.HideHeadFrame && SeenByOthers() == 0 && OwnBoard() == frames[1] &&
            Owned(frames[1]) == 1);

        DatabaseHelper.Instance!.SaveDatabase();
        await using (var db = new SqliteConnection($"Data Source={Path.Combine(scratch, "regression.db")}"))
        {
            await db.OpenAsync();
            var stored = EntityStores.Map[typeof(PlayerData)].LoadAll(db).OfType<PlayerData>()
                .Single(data => data.Uid == player.Uid);
            check("the hidden head-frame switch survives SQLite storage", stored.PrivacySettings.HideHeadFrame);
        }

        var show = await Setting(new UpdatePlayerSetting { BOMGNMDOGLM = false });
        check("showing the head frame again restores it for other players",
            show.PlayerSetting?.BKAGLMFNKKACase == UpdatePlayerSetting.BKAGLMFNKKAOneofCase.BOMGNMDOGLM &&
            !player.Data.PrivacySettings.HideHeadFrame && !HiddenAtLogin() && SeenByOthers() == frames[1]);

        var oldBlob = MemoryPackSerializer.Serialize(new PreHeadFramePrivacySettings
            { DisplayActiveState = false, DisplayCollection = false });
        check("a privacy-settings blob saved before the switch loads with the head frame shown",
            MemoryPackSerializer.Deserialize<PrivacySettingsPb>(oldBlob) is
            {
                HideHeadFrame: false, DisplayChallengeLineup: true, DisplayActiveState: false,
                DisplayRecentlyState: true, DisplayBattleRecord: true, DisplayCollection: false
            });

        // Old saves wore a frame without owning the item; login adds it so the client offers the switch.
        player.Data.HeadFrame = new PlayerHeadFrameInfo { HeadFrameId = frames[0], HeadFrameExpireTime = long.MaxValue };
        bag.Data.MaterialItems.RemoveAll(item => item.ItemId == frames[0]);
        await capture(async () => await player.OnLogin());
        check("login puts the worn head frame of an old save into the bag", Owned(frames[0]) == 1);
        await capture(async () => await player.OnLogin());
        check("a second login does not add the worn head frame twice", Owned(frames[0]) == 1);
        player.Connection = null;
    }
}

// Independent copy of the five-member privacy contract saved before the head-frame switch existed.
[MemoryPackable]
internal partial class PreHeadFramePrivacySettings
{
    public bool DisplayChallengeLineup { get; set; } = true;
    public bool DisplayActiveState { get; set; } = true;
    public bool DisplayRecentlyState { get; set; } = true;
    public bool DisplayBattleRecord { get; set; } = true;
    public bool DisplayCollection { get; set; } = true;
}

// Independent copy of the old five-member wire contract; does not derive its fixture from the new model.
[MemoryPackable]
internal partial class PreFavouriteLineup
{
    public string? Name { get; set; }
    public int LineupType { get; set; }
    public int LeaderAvatarId { get; set; }
    public List<LineupAvatarInfo>? BaseAvatars { get; set; }
    public int Mp { get; set; } = 5;
}
