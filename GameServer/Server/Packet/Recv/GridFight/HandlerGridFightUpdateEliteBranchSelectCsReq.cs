using March7thHoney.GameServer.Game.GridFight.Component;
using March7thHoney.GameServer.Game.GridFight.Sync;
using March7thHoney.GameServer.Server.Packet.Send.GridFight;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightUpdateEliteBranchSelectCsReq)]
public class HandlerGridFightUpdateEliteBranchSelectCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightUpdateEliteBranchSelectCsReq.Parser.ParseFrom(data);

        var inst = connection.Player!.GridFightManager!.GridFightInstance;
        if (inst == null)
        {
            await connection.SendPacket(CmdIds.GridFightUpdateEliteBranchSelectScRsp);
            return;
        }

        var component = inst.GetComponent<GridFightLevelComponent>();

        component.CurrentSection.BranchId = req.EliteBranchId;

        // sync
        await connection.SendPacket(
            new PacketGridFightSyncUpdateResultScNotify(new GridFightEliteBranchSyncData(GridFightSrc.KGridFightSrcNone, component.CurrentSection)));

        await connection.SendPacket(CmdIds.GridFightUpdateEliteBranchSelectScRsp);
    }
}