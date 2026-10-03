using March7thHoney.Enums.Scene;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.Rogue;
using March7thHoney.GameServer.Server.Packet.Send.Lineup;
using March7thHoney.GameServer.Server.Packet.Send.Rogue;
using March7thHoney.GameServer.Server.Packet.Send.Scene;
using March7thHoney.Kcp;
using March7thHoney.Proto;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Server.Packet.Recv.Rogue;

internal static class RogueResponseParts
{
    public static LineupInfo BuildLineup(PlayerInstance player)
    {
        var lineup = player.LineupManager!.GetCurLineup()!.ToProto();
        player.LineupManager.ApplyMaxMp(lineup);
        return lineup;
    }

    public static SceneInfo BuildScene(PlayerInstance player)
    {
        var scene = player.SceneInstance!;
        var info = scene.ToProto();
        scene.MarkClientSceneSnapshotSynced();
        return info;
    }
}

[Opcode(CmdIds.GetRogueInfoCsReq)]
public sealed class HandlerGetRogueInfoCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketRogueScRsp(CmdIds.GetRogueInfoScRsp,
            new GetRogueInfoScRsp { RogueGameInfo = player.RogueManager!.BuildRogueGameInfo() }));
    }
}

[Opcode(CmdIds.StartRogueCsReq)]
public sealed class HandlerStartRogueCsReq : Handler<StartRogueCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, StartRogueCsReq req)
    {
        var manager = player.RogueManager!;

        // Formal roster first; otherwise the trial or story-line roster.
        var avatarIds = req.NOEPBNMJFGL.Select(x => (int)x).ToList();
        var trial = avatarIds.Count == 0;
        if (trial) avatarIds = req.JKPIPNMGGOA.Select(x => (int)x).ToList();
        if (avatarIds.Count == 0) avatarIds = req.UnfinishedStoryLineIdList.Select(x => (int)x).ToList();

        var ret = await manager.StartRogue((int)req.AreaId, (int)req.AFEMBLFCCCH, avatarIds, trial);
        var inst = manager.RogueInstance;
        if (ret != Retcode.RetSucc || inst == null)
        {
            await connection.SendPacket(new PacketRogueScRsp(CmdIds.StartRogueScRsp,
                new StartRogueScRsp { Retcode = (uint)(ret == Retcode.RetSucc ? Retcode.RetFail : ret) }));
            return;
        }

        // Official order: coin result + sync, status, room as Unlock, scene change, room as Play, lineup,
        // response, then the map-refresh update after the response.
        await connection.SendPacket(new PacketSyncRogueCommonActionResultScNotify(inst.Coin));
        await connection.SendPacket(new PacketSyncRogueVirtualItemInfoScNotify(inst.Coin));
        await connection.SendPacket(new PacketSyncRogueStatusScNotify(inst.Status));
        await connection.SendPacket(new PacketSyncRogueMapRoomScNotify(inst, RogueRoomStatus.Unlock));
        await connection.SendPacket(new PacketSyncServerSceneChangeNotify());
        await connection.SendPacket(new PacketSyncRogueMapRoomScNotify(inst));
        await connection.SendPacket(new PacketSyncLineupNotify(player.LineupManager!.GetCurLineup()!));
        await connection.SendPacket(new PacketRogueScRsp(CmdIds.StartRogueScRsp, new StartRogueScRsp
        {
            RogueGameInfo = manager.BuildRogueGameInfo(),
            Lineup = RogueResponseParts.BuildLineup(player),
            Scene = RogueResponseParts.BuildScene(player)
        }));
        await connection.SendPacket(new PacketCommonRogueUpdateScNotify(inst.AreaId));
    }
}

[Opcode(CmdIds.EnterRogueMapRoomCsReq)]
public sealed class HandlerEnterRogueMapRoomCsReq : Handler<EnterRogueMapRoomCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, EnterRogueMapRoomCsReq req)
    {
        var manager = player.RogueManager!;
        var siteId = req.MAJKFIMPOOF != 0 ? req.MAJKFIMPOOF : req.RoomId;
        var (ret, room) = await manager.EnterRoom((int)siteId);
        if (ret != Retcode.RetSucc || room == null || manager.RogueInstance == null)
        {
            await connection.SendPacket(new PacketRogueScRsp(CmdIds.EnterRogueMapRoomScRsp,
                new EnterRogueMapRoomScRsp { Retcode = (uint)ret }));
            return;
        }

        await connection.SendPacket(new PacketSyncRogueMapRoomScNotify(manager.RogueInstance));
        await connection.SendPacket(new PacketSyncLineupNotify(player.LineupManager!.GetCurLineup()!));
        await connection.SendPacket(new PacketRogueScRsp(CmdIds.EnterRogueMapRoomScRsp, new EnterRogueMapRoomScRsp
        {
            ANHDBMOPGDC = (uint)room.SiteId,
            Lineup = RogueResponseParts.BuildLineup(player),
            Scene = RogueResponseParts.BuildScene(player)
        }));
    }
}

[Opcode(CmdIds.QuitRogueCsReq)]
public sealed class HandlerQuitRogueCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        var manager = player.RogueManager!;
        if (manager.RogueInstance == null)
        {
            await connection.SendPacket(new PacketRogueScRsp(CmdIds.QuitRogueScRsp, new QuitRogueScRsp()));
            return;
        }

        manager.QuitRogue();
        await connection.SendPacket(new PacketSyncRogueStatusScNotify(manager.RogueInstance.Status));
        await connection.SendPacket(new PacketRogueScRsp(CmdIds.QuitRogueScRsp,
            new QuitRogueScRsp { RogueGameInfo = manager.BuildRogueGameInfo() }));
    }
}

[Opcode(CmdIds.LeaveRogueCsReq)]
public sealed class HandlerLeaveRogueCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        var manager = player.RogueManager!;
        var inst = await manager.FinishRun();
        if (inst == null)
        {
            // No run to settle, but a room scene restored at login still has to return to the lobby.
            if (await manager.LeaveOrphanedRoom())
            {
                await SendLeft(connection, player);
                return;
            }

            await connection.SendPacket(new PacketRogueScRsp(CmdIds.LeaveRogueScRsp,
                new LeaveRogueScRsp { Retcode = (uint)Retcode.RetRogueStatusNotMatch }));
            return;
        }

        await connection.SendPacket(new PacketSyncRogueFinishScNotify(RogueManager.BuildFinishInfo(inst)));

        if (!await manager.LeaveRun())
        {
            await connection.SendPacket(new PacketRogueScRsp(CmdIds.LeaveRogueScRsp,
                new LeaveRogueScRsp { Retcode = (uint)Retcode.RetFail }));
            return;
        }

        await SendLeft(connection, player);
    }

    private static async Task SendLeft(Connection connection, PlayerInstance player)
    {
        // Without the scene-change signal the client can stay in the rogue-scene state after leaving.
        await connection.SendPacket(new PacketSyncServerSceneChangeNotify());
        await connection.SendPacket(new PacketRogueScRsp(CmdIds.LeaveRogueScRsp, new LeaveRogueScRsp
        {
            Lineup = RogueResponseParts.BuildLineup(player),
            Scene = RogueResponseParts.BuildScene(player)
        }));
    }
}

[Opcode(CmdIds.GetRogueTalentInfoCsReq)]
public sealed class HandlerGetRogueTalentInfoCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketRogueScRsp(CmdIds.GetRogueTalentInfoScRsp,
            new GetRogueTalentInfoScRsp { JCOBJAMPOAL = player.RogueManager!.BuildTalentInfo() }));
    }
}

[Opcode(CmdIds.EnableRogueTalentCsReq)]
public sealed class HandlerEnableRogueTalentCsReq : Handler<EnableRogueTalentCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, EnableRogueTalentCsReq req)
    {
        var manager = player.RogueManager!;
        var rsp = await manager.EnableTalent((int)req.CKHCLKOALKE)
            ? new EnableRogueTalentScRsp { JCOBJAMPOAL = manager.BuildTalentInfo() }
            : new EnableRogueTalentScRsp { Retcode = (uint)Retcode.RetItemNotEnough };
        await connection.SendPacket(new PacketRogueScRsp(CmdIds.EnableRogueTalentScRsp, rsp));
    }
}

[Opcode(CmdIds.GetRogueHandbookDataCsReq)]
public sealed class HandlerGetRogueHandbookDataCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketRogueScRsp(CmdIds.GetRogueHandbookDataScRsp,
            new GetRogueHandbookDataScRsp { HAMKPLCBEBG = RogueManager.BuildHandbookData() }));
    }
}

[Opcode(CmdIds.CommonRogueQueryCsReq)]
public sealed class HandlerCommonRogueQueryCsReq : Handler<CommonRogueQueryCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, CommonRogueQueryCsReq req)
    {
        var rsp = new CommonRogueQueryScRsp();
        rsp.LFLCDNGALME.AddRange(RogueManager.BuildCommonQueryEntries(req.AGFFNBFBHDL));
        await connection.SendPacket(new PacketRogueScRsp(CmdIds.CommonRogueQueryScRsp, rsp));
    }
}

// CycleScoreRewardGetData: feeds the Divergent Universe activity block's time window.
[Opcode(CmdIds.CycleScoreRewardGetDataCsReq)]
public sealed class HandlerCycleScoreRewardGetDataCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketRogueScRsp(CmdIds.CycleScoreRewardGetDataScRsp,
            new CycleScoreRewardGetDataScRsp { NFEDANFBIBE = RogueManager.BuildCycleScoreData() }));
    }
}

[Opcode(CmdIds.OFEDNNJIJFL)]
public sealed class HandlerHandleRogueCommonPendingActionCsReq : Handler<OFEDNNJIJFL>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, OFEDNNJIJFL req)
    {
        // Each mode only claims its own answer arms in its own scene and returns null otherwise.
        var modeRsp = player.RogueMagicManager != null ? await player.RogueMagicManager.HandlePendingAction(req) : null;
        if (modeRsp == null && player.ChessRogueManager != null)
            modeRsp = await player.ChessRogueManager.HandlePendingAction(req);
        if (modeRsp == null && player.RogueTournManager != null)
            modeRsp = await player.RogueTournManager.HandlePendingAction(req);
        if (modeRsp != null)
        {
            await connection.SendPacket(new PacketRogueScRsp(CmdIds.HandleRogueCommonPendingActionScRsp, modeRsp));
            return;
        }

        var manager = player.RogueManager!;
        var rsp = new HandleRogueCommonPendingActionScRsp();

        // Outside a Simulated Universe scene an unclaimed parameterless answer is another mode's arm, not a reroll.
        if (req.DLKIKBIDJDB == null && player.SceneInstance?.GameModeType == GameModeTypeEnum.RogueExplore &&
            IsRerollAnswer(req))
        {
            // The reroll callback (field 7) is the only response arm carrying the buff draft info.
            var info = manager.RerollBuff();
            if (info == null)
            {
                rsp.Retcode = (uint)Retcode.RetFail;
            }
            else
            {
                rsp.JFHKDEIDMOG = new FKOGDINOBDF { PCHGEGFBKLF = info };
                rsp.QueuePosition = req.MPLGEEGOAMC;
                rsp.BFNEPPCNOOI = req.MPLGEEGOAMC;
            }

            await connection.SendPacket(new PacketRogueScRsp(CmdIds.HandleRogueCommonPendingActionScRsp, rsp));
            return;
        }

        // Field 1588 is the buff-select answer (yuukisr OPDCGJEKEKP); other arms are unrelated actions.
        var buffId = req.DLKIKBIDJDB?.BAOFIGFFMLN ?? 0;
        var ret = manager.SelectBuff((int)buffId);

        if (ret is { } retcode and not Retcode.RetSucc)
        {
            rsp.Retcode = (uint)retcode;
        }
        else if (ret == Retcode.RetSucc)
        {
            // The official response echoes the queue position in both fields.
            rsp.QueuePosition = req.MPLGEEGOAMC;
            rsp.BFNEPPCNOOI = req.MPLGEEGOAMC;

            // The resolved draft clears the room: the official refreshes the exit portal Open before the response.
            await manager.OpenRoomExit();
        }

        await connection.SendPacket(new PacketRogueScRsp(CmdIds.HandleRogueCommonPendingActionScRsp, rsp));

        // A pick that completes a path-resonance threshold opens the aeon draft; the client needs it twice.
        if (ret == Retcode.RetSucc && manager.RogueInstance is { BuffSelect.HasSelection: true } inst)
        {
            await connection.SendPacket(new PacketSyncRogueCommonPendingActionScNotify(RogueManager.BuildPendingAction(inst)));
            if (GameConstants.IsVersion("4.5.0"))
                await connection.SendPacket(new PacketSyncRogueCommonPendingActionScNotify(RogueManager.BuildPendingAction(inst)));
        }
    }

    // Parameterless arms are reroll answers; a Simulated Universe run only holds a blessing draft to reroll.
    private static bool IsRerollAnswer(OFEDNNJIJFL req)
    {
        return req.KBNLGEMNDEN != null || req.KCPJPFIPBKM != null || req.KJFHBMICMME != null ||
               req.LGBJFCFLOGN != null || req.INPCEEAAKKG != null || req.HKJGJHCKJCJ != null ||
               req.JHGCIENPBBA != null;
    }
}

// Lobby queries the client sends before StartRogue; yuukisr answers them with an empty success.
[Opcode(CmdIds.GetRogueAeonInfoCsReq)]
public sealed class HandlerGetRogueAeonInfoCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketRogueScRsp(CmdIds.GetRogueAeonInfoScRsp, new GetRogueAeonInfoScRsp()));
    }
}

[Opcode(CmdIds.GetRogueBuffEnhanceInfoCsReq)]
public sealed class HandlerGetRogueBuffEnhanceInfoCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketRogueScRsp(CmdIds.GetRogueBuffEnhanceInfoScRsp,
            new GetRogueBuffEnhanceInfoScRsp()));
    }
}

// Classic Simulated Universe enhance is the 18xx pair (4.4 SimulatedRogueEnhanceRogueBuffCsReq, obf CINMCBDCBMJ /
// NAHANHPHGGF, 1885/1807 in 4.6); the 5484/5469 pair is the ChessRogue one. The generated EnhanceRogueBuffScRsp
// is the ChessRogue layout, which is fine here because an empty body serializes the same for both.
[Opcode(CmdIds.EnhanceRogueBuffCsReqCmd1885)]
public sealed class HandlerEnhanceRogueBuffCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketRogueScRsp(CmdIds.EnhanceRogueBuffScRspCmd1807, new EnhanceRogueBuffScRsp()));
    }
}

// Official 4.5 capture: each pick channel is echoed back, special ids (request #14) in response #9 and base
// ids (request #9) in response #2. An empty response leaves the client's picker waiting with input locked.
// Each channel goes back in the response field that shares its dumped name (4.6: request #11 -> response #6,
// request #14 -> response #15).
[Opcode(CmdIds.PickRogueAvatarCsReq)]
public sealed class HandlerPickRogueAvatarCsReq : Handler<PickRogueAvatarCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, PickRogueAvatarCsReq req)
    {
        var rsp = new PickRogueAvatarScRsp();
        rsp.BFEDFBEKOBI.AddRange(req.DFPPCAJDBFK);
        rsp.NOEPBNMJFGL.AddRange(req.NOEPBNMJFGL);
        await connection.SendPacket(new PacketRogueScRsp(CmdIds.PickRogueAvatarScRsp, rsp));
    }
}

[Opcode(CmdIds.GetRogueCommonDialogueDataCsReq)]
public sealed class HandlerGetRogueCommonDialogueDataCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        // A live Divergent Universe run answers with its Event-room talks (also the reconnect path).
        await connection.SendPacket(new PacketRogueScRsp(CmdIds.GetRogueCommonDialogueDataScRsp,
            player.RogueTournManager?.GetDialogueData() ?? new GetRogueCommonDialogueDataScRsp()));
    }
}

[Opcode(CmdIds.SelectRogueCommonDialogueOptionCsReq)]
public sealed class HandlerSelectRogueCommonDialogueOptionCsReq : Handler<SelectRogueCommonDialogueOptionCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player,
        SelectRogueCommonDialogueOptionCsReq req)
    {
        var tourn = player.RogueTournManager;
        var rsp = tourn == null ? null : await tourn.SelectDialogueOption((int)req.AIAFEKGBFIF, (int)req.HAFOKMHCGFM);
        await connection.SendPacket(new PacketRogueScRsp(CmdIds.SelectRogueCommonDialogueOptionScRsp,
            rsp ?? new SelectRogueCommonDialogueOptionScRsp()));
        // A pick that ended the talk clears it from the client's set and releases the room after the response.
        if (rsp != null) await tourn!.FlushDialogueClose();
    }
}

[Opcode(CmdIds.GetRogueEndlessActivityDataCsReq)]
public sealed class HandlerGetRogueEndlessActivityDataCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketRogueScRsp(CmdIds.GetRogueEndlessActivityDataScRsp,
            new GetRogueEndlessActivityDataScRsp()));
    }
}

[Opcode(CmdIds.RogueArcadeGetInfoCsReq)]
public sealed class HandlerRogueArcadeGetInfoCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketRogueScRsp(CmdIds.RogueArcadeGetInfoScRsp, new RogueArcadeGetInfoScRsp()));
    }
}
