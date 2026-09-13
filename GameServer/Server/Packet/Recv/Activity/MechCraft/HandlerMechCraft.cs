using March7thHoney.GameServer.Server.Packet.Send.Activity.MechCraft;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Activity.MechCraft;

[Opcode(CmdIds.MechCraftGetDataCsReq)]
public sealed class HandlerMechCraftGetDataCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketMechCraftData(CmdIds.MechCraftGetDataScRsp, player.MechCraftManager!.ToProto()));
    }
}

[Opcode(CmdIds.MechCraftActivateTalentPointCsReq)]
public sealed class HandlerMechCraftActivateTalentPointCsReq : Handler<MechCraftActivateTalentPointCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, MechCraftActivateTalentPointCsReq request)
    {
        await connection.SendPacket(new PacketMechCraftData(CmdIds.MechCraftActivateTalentPointScRsp,
            player.MechCraftManager!.ActivateTalentPoint(request.TalentPointId)));
    }
}

[Opcode(CmdIds.MechCraftSubmitOrderCsReq)]
public sealed class HandlerMechCraftSubmitOrderCsReq : Handler<MechCraftSubmitOrderCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, MechCraftSubmitOrderCsReq request)
    {
        var response = player.MechCraftManager!.SubmitOrder(request);
        await connection.SendPacket(new PacketMechCraftData(CmdIds.MechCraftSubmitOrderScRsp, response));
        if (response.Retcode != 0) return;
        var update = new MechCraftOrderUnlockUpdate();
        update.AvailableOrderIdList.Add(request.OrderId);
        var notify = new MechCraftUnlockScNotify();
        notify.UnlockUpdateList.Add(new MechCraftUnlockUpdate { OrderUpdate = update });
        await connection.SendPacket(new PacketMechCraftData(CmdIds.MechCraftUnlockScNotify, notify));
    }
}

[Opcode(CmdIds.MechCraftSubmitExamCsReq)]
public sealed class HandlerMechCraftSubmitExamCsReq : Handler<MechCraftSubmitExamCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, MechCraftSubmitExamCsReq request)
    {
        await connection.SendPacket(new PacketMechCraftData(CmdIds.MechCraftSubmitExamScRsp, player.MechCraftManager!.SubmitExam(request)));
    }
}

[Opcode(CmdIds.MechCraftSaveDiyCsReq)]
public sealed class HandlerMechCraftSaveDiyCsReq : Handler<MechCraftSaveDiyCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, MechCraftSaveDiyCsReq request)
    {
        await connection.SendPacket(new PacketMechCraftData(CmdIds.MechCraftSaveDiyScRsp,
            new MechCraftSaveDiyScRsp { Retcode = (uint)player.MechCraftManager!.SaveDiy(request) }));
    }
}

[Opcode(CmdIds.MechCraftFinishReportCsReq)]
public sealed class HandlerMechCraftFinishReportCsReq : Handler<MechCraftFinishReportCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, MechCraftFinishReportCsReq request)
    {
        await connection.SendPacket(new PacketMechCraftData(CmdIds.MechCraftFinishReportScRsp,
            new MechCraftFinishReportScRsp { Retcode = (uint)player.MechCraftManager!.FinishReport(request.ReportId) }));
    }
}

[Opcode(CmdIds.MechCraftReportGameStateCsReq)]
public sealed class HandlerMechCraftReportGameStateCsReq : Handler<MechCraftReportGameStateCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, MechCraftReportGameStateCsReq request)
    {
        await connection.SendPacket(new PacketMechCraftData(CmdIds.MechCraftReportGameStateScRsp,
            new MechCraftReportGameStateScRsp { Retcode = (uint)player.MechCraftManager!.ReportGameState(request) }));
    }
}
