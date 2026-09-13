using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.RogueTourn;
using March7thHoney.GameServer.Server.Packet.Send.RogueTourn;
using March7thHoney.GameServer.Server.Packet.Send.Scene;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.RogueTourn;

// The Divergent Universe home pull; an unpopulated body NREs the client.
[Opcode(CmdIds.RogueTournQueryCsReq)]
public sealed class HandlerRogueTournQueryCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        var manager = player.RogueTournManager!;
        var rsp = new RogueTournQueryScRsp { LFLCDNGALME = RogueTournProtoBuilder.BuildQueryInfo(manager.Data) };
        if (manager.RunInstance != null) rsp.PHGJIJELKDB = RogueTournProtoBuilder.BuildRunInfo(manager.RunInstance);
        await connection.SendPacket(new PacketRogueTourn(CmdIds.RogueTournQueryScRsp, rsp));
    }
}

[Opcode(CmdIds.RogueTournStartCsReq)]
public sealed class HandlerRogueTournStartCsReq : Handler<RogueTournStartCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, RogueTournStartCsReq req)
    {
        var manager = player.RogueTournManager!;
        var avatarIds = req.AvatarList.Select(avatar => (int)avatar.Id).ToList();
        var (ret, run) = await manager.StartRun((int)req.AreaId, avatarIds);
        if (ret != Retcode.RetSucc || run == null)
        {
            await connection.SendPacket(new PacketRogueTourn(CmdIds.RogueTournStartScRsp,
                new RogueTournStartScRsp { Retcode = (uint)ret }));
            return;
        }

        if (!await manager.EnterRoomScene(run.CurRoom(), false))
        {
            await connection.SendPacket(new PacketRogueTourn(CmdIds.RogueTournStartScRsp,
                new RogueTournStartScRsp { Retcode = (uint)Retcode.RetFail }));
            return;
        }

        // Official order: level notify, the opening pending action, then the response mirroring it.
        await manager.SendLevelNotify();
        await manager.SendPendingNotify();
        await connection.SendPacket(new PacketRogueTourn(CmdIds.RogueTournStartScRsp, new RogueTournStartScRsp
        {
            IDCMACCPCBH = RogueTournProtoBuilder.BuildEnterInfo(player),
            PHGJIJELKDB = RogueTournProtoBuilder.BuildRunInfo(run)
        }));
    }
}

// Resume path: re-sends the live run.
[Opcode(CmdIds.RogueTournEnterCsReq)]
public sealed class HandlerRogueTournEnterCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        var run = player.RogueTournManager!.RunInstance;
        var rsp = run == null
            ? new RogueTournEnterScRsp { Retcode = (uint)Retcode.RetRogueStatusNotMatch }
            : new RogueTournEnterScRsp { PHGJIJELKDB = RogueTournProtoBuilder.BuildRunInfo(run) };
        await connection.SendPacket(new PacketRogueTourn(CmdIds.RogueTournEnterScRsp, rsp));
    }
}

// The response must carry the lobby scene, or the client keeps the dead run scene and retries forever.
[Opcode(CmdIds.RogueTournLeaveCsReq)]
public sealed class HandlerRogueTournLeaveCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await player.RogueTournManager!.LeaveRun();

        var (success, _) = await player.TryEnterScene(RogueTournManager.RogueLobbyEntranceId, 0, false);
        if (!success)
        {
            await connection.SendPacket(new PacketRogueTourn(CmdIds.RogueTournLeaveScRsp,
                new RogueTournLeaveScRsp { Retcode = (uint)Retcode.RetFail }));
            return;
        }

        // Tears the client's tourn maze state down before the entrance scene arrives.
        await connection.SendPacket(new PacketSyncServerSceneChangeNotify());
        await connection.SendPacket(new PacketRogueTourn(CmdIds.RogueTournLeaveScRsp,
            new RogueTournLeaveScRsp { IDCMACCPCBH = RogueTournProtoBuilder.BuildEnterInfo(player) }));
    }
}

[Opcode(CmdIds.RogueTournEnterRoomCsReq)]
public sealed class HandlerRogueTournEnterRoomCsReq : Handler<RogueTournEnterRoomCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, RogueTournEnterRoomCsReq req)
    {
        var manager = player.RogueTournManager!;
        var ret = manager.EnterRoom((int)req.KHBLMAOPLLN);
        if (ret == Retcode.RetSucc && manager.RunInstance == null) ret = Retcode.RetRogueStatusNotMatch;
        if (ret != Retcode.RetSucc)
        {
            await connection.SendPacket(new PacketRogueTourn(CmdIds.RogueTournEnterRoomScRsp,
                new RogueTournEnterRoomScRsp { Retcode = (uint)ret }));
            return;
        }

        await manager.EnterRoomScene(manager.RunInstance!.CurRoom(), false);
        await manager.SendLevelNotify();
        await connection.SendPacket(new PacketRogueTourn(CmdIds.RogueTournEnterRoomScRsp,
            new RogueTournEnterRoomScRsp { IDCMACCPCBH = RogueTournProtoBuilder.BuildEnterInfo(player) }));
    }
}

[Opcode(CmdIds.RogueTournEnterLayerCsReq)]
public sealed class HandlerRogueTournEnterLayerCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        var manager = player.RogueTournManager!;
        var ret = manager.EnterLayer();
        if (ret == Retcode.RetSucc && manager.RunInstance == null) ret = Retcode.RetRogueStatusNotMatch;
        if (ret != Retcode.RetSucc)
        {
            await connection.SendPacket(new PacketRogueTourn(CmdIds.RogueTournEnterLayerScRsp,
                new RogueTournEnterLayerScRsp { Retcode = (uint)ret }));
            return;
        }

        await manager.EnterRoomScene(manager.RunInstance!.CurRoom(), false);
        await manager.SendLevelNotify();
        await connection.SendPacket(new PacketRogueTourn(CmdIds.RogueTournEnterLayerScRsp,
            new RogueTournEnterLayerScRsp { IDCMACCPCBH = RogueTournProtoBuilder.BuildEnterInfo(player) }));
    }
}

/// <summary>
///     RogueTournTriggerRoomSelectCsReq (6016, obfuscated in 4.5). Fired after every room load and again on the
///     far side of an opened door; without the acknowledgement the room transition never completes.
/// </summary>
[Opcode(CmdIds.NPOJNHKPKMN)]
public sealed class HandlerRogueTournTriggerRoomSelectCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        var manager = player.RogueTournManager!;
        if (manager.RunInstance == null)
        {
            await connection.SendPacket(new PacketRogueTourn(CmdIds.MNILJCPCOFG,
                new MNILJCPCOFG { Retcode = (uint)Retcode.RetRogueStatusNotMatch }));
            return;
        }

        var (ret, next, moved) = manager.SelectNextRoom();
        if (ret != Retcode.RetSucc)
        {
            await connection.SendPacket(new PacketRogueTourn(CmdIds.MNILJCPCOFG, new MNILJCPCOFG { Retcode = (uint)ret }));
            return;
        }

        // Room blocks share one floor, so a room change re-enters the same plane at the next block's anchor.
        if (moved && next != null) await manager.EnterRoomScene(next, true);

        await manager.SendLevelNotify();
        await manager.SendPendingNotify();
        await connection.SendPacket(new PacketRogueTourn(CmdIds.MNILJCPCOFG, new MNILJCPCOFG()));
    }
}

[Opcode(CmdIds.RogueTournGetPermanentTalentInfoCsReq)]
public sealed class HandlerRogueTournGetPermanentTalentInfoCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketRogueTourn(CmdIds.RogueTournGetPermanentTalentInfoScRsp,
            new RogueTournGetPermanentTalentInfoScRsp
            {
                DPABCOHLPLH = RogueTournProtoBuilder.BuildPermanentTalentInfo(player.RogueTournManager!.Data)
            }));
    }
}

[Opcode(CmdIds.RogueTournGetSeasonTalentInfoCsReq)]
public sealed class HandlerRogueTournGetSeasonTalentInfoCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketRogueTourn(CmdIds.RogueTournGetSeasonTalentInfoScRsp,
            new RogueTournGetSeasonTalentInfoScRsp
            {
                HANICMMHJGI = RogueTournProtoBuilder.BuildSeasonTalentInfo(player.RogueTournManager!.Data)
            }));
    }
}

[Opcode(CmdIds.RogueTournGetAllArchiveCsReq)]
public sealed class HandlerRogueTournGetAllArchiveCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        var rsp = new RogueTournGetAllArchiveScRsp();
        foreach (var entry in player.RogueTournManager!.Data.Archives)
            rsp.MCGGPONOCKC.Add(RogueTournProtoBuilder.BuildArchive(entry));
        await connection.SendPacket(new PacketRogueTourn(CmdIds.RogueTournGetAllArchiveScRsp, rsp));
    }
}

// No collection tracker yet: both catalogue lists stay empty (a fresh account's shape).
[Opcode(CmdIds.RogueTournGetArchiveRepositoryCsReq)]
public sealed class HandlerRogueTournGetArchiveRepositoryCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketRogueTourn(CmdIds.RogueTournGetArchiveRepositoryScRsp,
            new RogueTournGetArchiveRepositoryScRsp()));
    }
}

// The official capture answers an empty list; build-ref sharing is partly an HTTP feature.
[Opcode(CmdIds.RogueTournGetAllBuildRefCsReq)]
public sealed class HandlerRogueTournGetAllBuildRefCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketRogueTourn(CmdIds.RogueTournGetAllBuildRefScRsp,
            new RogueTournGetAllBuildRefScRsp()));
    }
}

[Opcode(CmdIds.RogueTournGetMiscRealTimeDataCsReq)]
public sealed class HandlerRogueTournGetMiscRealTimeDataCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        var rsp = new RogueTournGetMiscRealTimeDataScRsp();
        RogueTournProtoBuilder.FillMiscRealTimeData(rsp);
        await connection.SendPacket(new PacketRogueTourn(CmdIds.RogueTournGetMiscRealTimeDataScRsp, rsp));
    }
}

// The Cocoon hub is not modelled; an empty body is the reference shape.
[Opcode(CmdIds.RogueTournGetCurRogueCocoonInfoCsReq)]
public sealed class HandlerRogueTournGetCurRogueCocoonInfoCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketRogueTourn(CmdIds.RogueTournGetCurRogueCocoonInfoScRsp,
            new RogueTournGetCurRogueCocoonInfoScRsp()));
    }
}
