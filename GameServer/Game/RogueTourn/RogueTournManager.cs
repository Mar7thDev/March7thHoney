using March7thHoney.Enums.Mission;
using March7thHoney.Data;
using March7thHoney.Data.Excel;
using March7thHoney.Database;
using March7thHoney.Database.RogueTourn;
using March7thHoney.Enums.Scene;
using March7thHoney.Enums.TournRogue;
using March7thHoney.GameServer.Game.Battle;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.Scene;
using March7thHoney.GameServer.Server.Packet.Send.RogueTourn;
using March7thHoney.Kcp;
using March7thHoney.Proto;
using MemoryPack;

namespace March7thHoney.GameServer.Game.RogueTourn;

public class RogueTournManager : BasePlayerManager
{
    // The rogue lobby floor; the Divergent Universe entrance lives there too.
    public const int RogueLobbyEntranceId = 801120102;

    // Official StartScRsp ships the tourn lineup at mp 5.
    private const int TournLineupMp = 5;

    // Capture: 50 Cosmic Fragments at run start.
    private const int StartCoinCount = 50;

    // The official three-mob combat room pays 15.
    private const int CoinPerMonster = 5;

    // The official run opens with three Epic Equations of its season.
    private const int StarterFormulaCount = 3;

    // Sizes a layer that ships no RogueTournLayerRoom rows (capture: 4 / 5 / 4).
    private const int DefaultLayerRoomCount = 4;

    public RogueTournManager(PlayerInstance player) : base(player)
    {
        Data = DatabaseHelper.Instance!.GetInstanceOrCreateNew<RogueTournData>(player.Uid);
        ResurrectRun();
    }

    public RogueTournData Data { get; }
    public RogueTournRunData? RunInstance { get; private set; }

    #region Persistence

    public void Save()
    {
        Data.RunInstance = RunInstance == null
            ? null
            : Convert.ToBase64String(MemoryPackSerializer.Serialize(RunInstance));
        DatabaseHelper.MarkDirty(Player.Uid);
    }

    private void ResurrectRun()
    {
        if (string.IsNullOrEmpty(Data.RunInstance)) return;

        RogueTournRunData? run;
        try
        {
            run = MemoryPackSerializer.Deserialize<RogueTournRunData>(Convert.FromBase64String(Data.RunInstance));
        }
        catch
        {
            // Incompatible/corrupt blob — drop it rather than crash the manager.
            run = null;
        }

        if (run == null || run.Status != RogueTournStates.RunDoing)
        {
            Data.RunInstance = null;
            return;
        }

        // Heal a persisted style action on a run whose game data carries no style object: it can never
        // be resolved and wedges the client's action queue.
        if (run.Pending?.Kind == RogueTournPendingKind.Style && !UsesPersonaStyle(run))
        {
            run.Pending = null;
            OpenFormulaDraft(run);
        }

        RunInstance = run;
    }

    #endregion

    #region Run Lifecycle

    public async ValueTask<(Retcode Ret, RogueTournRunData? Run)> StartRun(int areaId, List<int> avatarIds)
    {
        if (RunInstance != null) await ClearRun();

        if (!GameData.RogueTournAreaData.TryGetValue(areaId, out var area))
            return (Retcode.RetRogueAreaInvalid, null);

        var run = new RogueTournRunData
        {
            AreaId = areaId,
            TournMode = (int)area.TournMode,
            DifficultyId = area.DifficultyIDList.FirstOrDefault(),
            ItemValue = { [RogueTournStates.ItemValueCosmicFragment] = StartCoinCount },
            Status = RogueTournStates.RunDoing,
            LevelStatus = RogueTournStates.LevelProcessing,
            LevelReason = RogueTournStates.ReasonNone,
            PersonaStyle = Data.PersonaStyle != 0 ? Data.PersonaStyle : RogueTournResources.PersonaStyleDefault(),
            BaseAvatarIds = avatarIds,
            TransNo = Guid.NewGuid().ToString()
        };

        var ret = BuildLayers(run, area);
        if (ret != Retcode.RetSucc) return (ret, null);
        if (run.Layers.Count == 0) return (Retcode.RetRogueAreaInvalid, null);

        run.CurLayerIndex = 1;
        run.Layers[0].Status = RogueTournStates.LayerProcessing;
        if (!EnterFirstRoom(run)) return (Retcode.RetRogueAreaInvalid, null);

        // Only a Persona run can carry the opening style select; every other run opens its starter
        // Equation draft directly.
        if (UsesPersonaStyle(run))
            OpenStyleAction(run);
        else
            OpenFormulaDraft(run);

        if (!await ActivateTournLineup(avatarIds)) return (Retcode.RetRogueStatusNotMatch, null);

        RunInstance = run;
        Save();
        return (Retcode.RetSucc, run);
    }

    /// <summary>
    ///     A layer resolves its scene from the season's authored plan (Config/Custom/RogueTournRoomPlan.json),
    ///     or from the shipped room-template plane for its index. Rooms come from the plan's explicit types,
    ///     else a generated sequence sized by the layer's RogueTournLayerRoom rows.
    /// </summary>
    private static Retcode BuildLayers(RogueTournRunData run, RogueTournAreaExcel area)
    {
        if (area.LayerIDList.Count == 0) return Retcode.RetRogueAreaInvalid;

        var season = RogueTournResources.SeasonPlan(area.TournMode);
        for (var i = 0; i < area.LayerIDList.Count; i++)
        {
            var layerId = area.LayerIDList[i];
            var planeIndex = i + 1;
            var plan = season?.Plane(planeIndex);
            var layer = new RogueTournLayerData { LayerId = layerId, LayerIndex = planeIndex };
            var template = false;

            if (plan != null)
            {
                layer.PlaneId = plan.PlaneID;
                layer.EntranceId = plan.EntranceID;
                layer.AnchorGroup = plan.AnchorGroup;
            }
            else if (RogueTournResources.GuidePlane(planeIndex) is var guidePlane and not 0)
            {
                template = true;
                layer.PlaneId = guidePlane;
            }

            if (layer.PlaneId == 0) return Retcode.RetRogueAreaInvalid;
            if (layer.EntranceId == 0) layer.EntranceId = RogueTournResources.PlaneEntranceId(layer.PlaneId);
            if (layer.AnchorGroup == 0) layer.AnchorGroup = RogueTournResources.PlaneAnchorGroup(layer.PlaneId);

            // A template plane only hosts the room types it authors (it ships no Reward room, for instance).
            List<int> pool = [];
            if (template)
            {
                pool = RogueTournResources.RoomTypes(layer.PlaneId);
                if (pool.Count == 0) return Retcode.RetRogueAreaInvalid;
            }

            var types = RoomTypeSequence(layerId, planeIndex, area.LayerIDList.Count, plan, pool);
            for (var n = 0; n < types.Count; n++)
            {
                var roomIndex = n + 1;
                var roomType = types[n];
                // Spawn inside the block the scene will load for this room index, or the player lands in an
                // unloaded part of the plane.
                var anchorGroup = layer.AnchorGroup;
                var blockArea = RogueTournResources.RoomBlockArea(layer.PlaneId, roomIndex);
                if (blockArea != 0)
                    anchorGroup = blockArea;
                else if (template)
                    anchorGroup = RogueTournResources.RoomAnchorGroup(layer.PlaneId, roomType, anchorGroup);

                layer.Rooms.Add(new RogueTournRoomData
                {
                    RoomId = layer.EntranceId * 100 + roomIndex,
                    RoomIndex = roomIndex,
                    RoomType = roomType,
                    Status = RogueTournStates.RoomNone,
                    PlaneId = layer.PlaneId,
                    EntranceId = layer.EntranceId,
                    AnchorGroup = anchorGroup,
                    CompositionType = roomType,
                    Level = 1
                });
            }

            if (layer.Rooms.Count == 0) return Retcode.RetRogueAreaInvalid;
            run.Layers.Add(layer);
        }

        return Retcode.RetSucc;
    }

    private static List<int> RoomTypeSequence(int layerId, int planeIndex, int layerCount, RogueTournPlanePlan? plan,
        List<int> pool)
    {
        if (plan is { RoomTypes.Count: > 0 }) return [.. plan.RoomTypes];

        var count = DefaultLayerRoomCount;
        var rows = RogueTournResources.RoomsOfLayerCount(layerId);
        if (rows > 0)
            count = rows;
        else if (layerCount >= 3 && planeIndex == 2)
            count = 5;

        return GeneratedRoomTypes(count, pool);
    }

    // Opens on a battle, closes on the Boss, cycles the pool in between.
    private static List<int> GeneratedRoomTypes(int count, List<int> pool)
    {
        if (count <= 0) return [];
        if (pool.Count == 0)
            pool =
            [
                (int)RogueTournRoomTypeEnum.Battle, (int)RogueTournRoomTypeEnum.Event,
                (int)RogueTournRoomTypeEnum.Reward, (int)RogueTournRoomTypeEnum.Encounter,
                (int)RogueTournRoomTypeEnum.Coin, (int)RogueTournRoomTypeEnum.Boss
            ];

        var types = new List<int>(count);
        for (var n = 0; n < count; n++) types.Add(pool[n % pool.Count]);
        if (pool.Contains((int)RogueTournRoomTypeEnum.Battle)) types[0] = (int)RogueTournRoomTypeEnum.Battle;
        if (pool.Contains((int)RogueTournRoomTypeEnum.Boss)) types[count - 1] = (int)RogueTournRoomTypeEnum.Boss;
        return types;
    }

    private static bool EnterFirstRoom(RogueTournRunData run)
    {
        var layer = run.CurLayer();
        if (layer == null || layer.Rooms.Count == 0) return false;
        layer.CurRoomIndex = 1;
        layer.Rooms[0].Status = RogueTournStates.RoomProcessing;
        return true;
    }

    public Retcode EnterRoom(int roomIndex)
    {
        var layer = RunInstance?.CurLayer();
        if (layer == null) return Retcode.RetRogueStatusNotMatch;

        if (roomIndex == 0) roomIndex = layer.CurRoomIndex + 1;
        if (roomIndex <= 0 || roomIndex > layer.Rooms.Count) return Retcode.RetRogueStatusNotMatch;

        foreach (var room in layer.Rooms.Where(room => room.Status == RogueTournStates.RoomProcessing))
            room.Status = RogueTournStates.RoomFinish;

        layer.CurRoomIndex = roomIndex;
        layer.Rooms[roomIndex - 1].Status = RogueTournStates.RoomProcessing;
        Save();
        return Retcode.RetSucc;
    }

    /// <summary>
    ///     The walk the client's room-select trigger drives. The client fires it after loading every room,
    ///     so it only advances when the room the player stands in is already cleared.
    /// </summary>
    public (Retcode Ret, RogueTournRoomData? Room, bool Moved) SelectNextRoom()
    {
        var run = RunInstance;
        var room = run?.CurRoom();
        if (run == null || room == null) return (Retcode.RetRogueStatusNotMatch, null, false);
        if (room.Status != RogueTournStates.RoomFinish) return (Retcode.RetSucc, room, false);

        var layer = run.CurLayer();
        if (layer == null) return (Retcode.RetRogueStatusNotMatch, null, false);

        if (layer.CurRoomIndex < layer.Rooms.Count)
        {
            var ret = EnterRoom(0);
            return ret != Retcode.RetSucc ? (ret, null, false) : (Retcode.RetSucc, run.CurRoom(), true);
        }

        // Past the last room of the last layer the run is left untouched; the settle path owns that.
        if (run.CurLayerIndex < run.Layers.Count && EnterLayer() == Retcode.RetSucc && run.CurRoom() is { } next)
            return (Retcode.RetSucc, next, true);

        return (Retcode.RetSucc, room, false);
    }

    public Retcode EnterLayer()
    {
        var run = RunInstance;
        if (run == null || run.CurLayerIndex >= run.Layers.Count) return Retcode.RetRogueStatusNotMatch;

        var current = run.CurLayer();
        if (current != null) current.Status = RogueTournStates.LayerFinish;

        run.CurLayerIndex++;
        var next = run.CurLayer();
        if (next == null) return Retcode.RetRogueStatusNotMatch;

        next.Status = RogueTournStates.LayerProcessing;
        if (!EnterFirstRoom(run)) return Retcode.RetRogueAreaInvalid;

        Save();
        return Retcode.RetSucc;
    }

    public async ValueTask LeaveRun()
    {
        var hadRun = RunInstance != null;
        await ClearRun();
        if (hadRun) await Player.MissionManager!.TriggerCountEvent(MissionFinishTypeEnum.RogueFinishCnt);
    }

    private async ValueTask ClearRun()
    {
        RunInstance = null;
        await Player.LineupManager!.SetExtraLineup(ExtraLineupType.LineupNone, false);
        Save();
    }

    private async ValueTask<bool> ActivateTournLineup(List<int> avatarIds)
    {
        var lineupManager = Player.LineupManager!;
        lineupManager.SetExtraLineup(ExtraLineupType.LineupTournRogue, avatarIds);
        var lineup = lineupManager.GetExtraLineup(ExtraLineupType.LineupTournRogue);
        if (lineup == null || lineup.BaseAvatars?.Count == 0) return false;

        lineup.Mp = TournLineupMp;
        foreach (var avatarId in avatarIds)
        {
            var avatar = Player.AvatarManager!.GetFormalAvatar(avatarId);
            avatar?.SetCurHp(10000, true);
            avatar?.SetCurSp(5000, true);
        }

        await lineupManager.SetExtraLineup(ExtraLineupType.LineupTournRogue, false);
        return true;
    }

    #endregion

    #region Scene

    /// <summary>
    ///     Entity loader for a Rogue plane scene that belongs to the current room, or null for any other
    ///     Rogue plane. Also stamps the scene's game mode (TournRogue = 17; the plane's own type is 5).
    /// </summary>
    public SceneEntityLoader? CreateEntityLoader(SceneInstance scene)
    {
        var room = RunInstance?.Status == RogueTournStates.RunDoing ? RunInstance.CurRoom() : null;
        if (room == null || room.PlaneId != scene.PlaneId) return null;

        scene.GameModeType = GameModeTypeEnum.TournRogue;
        return new RogueTournEntityLoader(scene, room);
    }

    public async ValueTask<bool> EnterRoomScene(RogueTournRoomData? room, bool sendPacket)
    {
        if (room == null || room.EntranceId == 0) return false;
        if (!GameData.MapEntranceData.TryGetValue(room.EntranceId, out var entrance)) return false;

        GameData.GetFloorInfo(entrance.PlaneID, entrance.FloorID, out var floor);
        var anchorId = floor != null && floor.Groups.TryGetValue(room.AnchorGroup, out var group)
            ? group.AnchorList.FirstOrDefault()?.ID ?? 0
            : 0;

        await Player.EnterSceneByEntranceId(room.EntranceId, room.AnchorGroup, anchorId, sendPacket);
        return Player.SceneInstance?.PlaneId == entrance.PlaneID && Player.Data.EntryId == room.EntranceId;
    }

    #endregion

    #region Battle

    public void OnBattleStart(BattleInstance battle)
    {
        var run = RunInstance;
        if (run == null || run.Status != RogueTournStates.RunDoing) return;
        // A persisted run must not claim ordinary world fights fought outside the tourn rooms.
        if (Player.SceneInstance?.GameModeType != GameModeTypeEnum.TournRogue) return;

        // Room kills pay the run currency, never the open-world drop table.
        battle.IsTournRogue = true;
        battle.OnBattleEnd += OnBattleEnd;
    }

    // A loss is not settled: the Divergent Universe flow returns the player to the room.
    private async ValueTask OnBattleEnd(BattleInstance battle, PVEBattleResultCsReq req)
    {
        var run = RunInstance;
        if (run == null || run.Status != RogueTournStates.RunDoing) return;
        if (req.EndStatus != BattleEndStatus.BattleEndWin) return;

        var room = run.CurRoom();
        if (room == null) return;

        run.AddCoin(CoinPerMonster * Math.Max(1, battle.EntityMonsters.Count));
        if (room.Status == RogueTournStates.RoomProcessing) room.Status = RogueTournStates.RoomFinish;
        OpenFormulaDraft(run);
        Save();

        // Official order: the purse, the next draft and the level update all precede PVEBattleResultScRsp.
        await Player.SendPacket(new PacketRogueTourn(CmdIds.CommonRogueVirtualItemInfoScNotify,
            RogueTournProtoBuilder.BuildVirtualItemNotify(run)));
        await SendPendingNotify();
        await SendLevelNotify();
    }

    #endregion

    #region Pending Action

    // Only the Arcadian Chronicles season (Tourn3) carries a Persona style, and a Guide-group area stages
    // on the Guide game data even when its mode is Tourn3; sending the style select there NREs the client.
    public static bool UsesPersonaStyle(RogueTournRunData run)
    {
        if (run.TournMode != (int)RogueTournModeEnum.Tourn3) return false;
        return !(GameData.RogueTournAreaData.TryGetValue(run.AreaId, out var area) &&
                 area.AreaGroupID == RogueTournAreaGroupIDEnum.Guide);
    }

    private static void OpenStyleAction(RogueTournRunData run)
    {
        if (run.PersonaStyle == 0) return;
        run.Pending = new RogueTournPendingActionData
        {
            QueuePos = run.NextActionPos(),
            Kind = RogueTournPendingKind.Style,
            Options = [run.PersonaStyle]
        };
    }

    // The window rotates by queue position so consecutive drafts differ while staying deterministic.
    private static void OpenFormulaDraft(RogueTournRunData run)
    {
        var owned = run.Formulas.ToHashSet();
        var pool = RogueTournResources.FormulaIds((RogueTournModeEnum)run.TournMode, RogueFormulaCategoryEnum.Epic)
            .Where(id => !owned.Contains(id))
            .ToList();
        if (pool.Count == 0) return;

        var start = pool.Count > StarterFormulaCount ? run.ActionSeq % pool.Count : 0;
        List<int> options = [];
        for (var n = 0; n < StarterFormulaCount && n < pool.Count; n++) options.Add(pool[(start + n) % pool.Count]);

        run.Pending = new RogueTournPendingActionData
        {
            QueuePos = run.NextActionPos(),
            Kind = RogueTournPendingKind.Formula,
            Options = options
        };
    }

    /// <summary>
    ///     Resolves a HandleRogueCommonPendingActionCsReq (5611) answer to an open Divergent Universe action.
    ///     Returns null when the run does not own the answer, so the caller falls through to classic Rogue.
    ///     Pushes the result and next-action notifies itself; the caller only sends the returned ScRsp.
    /// </summary>
    public async ValueTask<HandleRogueCommonPendingActionScRsp?> HandlePendingAction(OFEDNNJIJFL req)
    {
        var run = RunInstance;
        if (run == null) return null;

        RogueTournPendingKind kind;
        int value;
        if (req.DPAMEGCFNAJ != null)
        {
            kind = RogueTournPendingKind.Style;
            value = (int)req.DPAMEGCFNAJ.EOOLCIACHFJ;
        }
        else if (req.POHIHFCHKNP != null)
        {
            kind = RogueTournPendingKind.Formula;
            value = (int)req.POHIHFCHKNP.JMNMFMHPCKD;
        }
        else
        {
            return null;
        }

        var pending = run.Pending;
        if (pending == null || pending.Kind != kind) return null;

        // A stale queue position is accepted while the arm still matches: the client re-sends mid-refresh.
        var queuePos = req.MPLGEEGOAMC;
        var ret = Retcode.RetSucc;
        List<RogueTournActionResult> results = [];

        if (!pending.Options.Contains(value))
        {
            // A bogus option must still consume the action or the confirm dialog wedges forever.
            run.Pending = null;
            ret = Retcode.RetRogueSelectBuffNotExist;
        }
        else if (kind == RogueTournPendingKind.Style)
        {
            run.PersonaStyle = value;
            Data.PersonaStyle = value;
            run.Pending = null;
            results.Add(new RogueTournActionResult(RogueTournActionResultKind.Style, value));
            // The official run follows the style confirmation with the starter Equation draft.
            OpenFormulaDraft(run);
        }
        else
        {
            if (!run.Formulas.Contains(value)) run.Formulas.Add(value);
            run.Pending = null;
            results.Add(new RogueTournActionResult(RogueTournActionResultKind.Formula, value));
        }

        Save();

        // Official order: result notifies, then the next action, then the response. Without the result
        // push the client re-sends the same answer every ~150 ms.
        foreach (var result in results)
            await Player.SendPacket(new PacketRogueTourn(CmdIds.SyncRogueCommonActionResultScNotify,
                RogueTournProtoBuilder.BuildActionResult(result)));
        await SendPendingNotify();

        // The official response echoes the position in both fields.
        return new HandleRogueCommonPendingActionScRsp
        {
            Retcode = (uint)ret,
            QueuePosition = queuePos,
            BFNEPPCNOOI = queuePos
        };
    }

    #endregion

    #region Notify

    public async ValueTask SendLevelNotify()
    {
        if (RunInstance == null) return;
        await Player.SendPacket(new PacketRogueTourn(CmdIds.RogueTournLevelInfoUpdateScNotify,
            RogueTournProtoBuilder.BuildLevelNotify(RunInstance)));
    }

    public async ValueTask SendPendingNotify()
    {
        if (RunInstance?.Pending == null) return;
        await Player.SendPacket(new PacketRogueTourn(CmdIds.SyncRogueCommonPendingActionScNotify,
            RogueTournProtoBuilder.BuildPendingNotify(RunInstance)));
    }

    #endregion
}
