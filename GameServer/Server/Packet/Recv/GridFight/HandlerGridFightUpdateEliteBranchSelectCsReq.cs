using CastoriceHoney.Server.GameServer.Game.GridFight.Component;
using CastoriceHoney.Server.GameServer.Game.GridFight.Sync;
using CastoriceHoney.Server.GameServer.Server.Packet.Send.GridFight;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.GridFight;

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