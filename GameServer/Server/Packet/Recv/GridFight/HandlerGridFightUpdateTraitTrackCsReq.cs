using CastoriceHoney.Server.GameServer.Game.GridFight.Component;
using CastoriceHoney.Server.GameServer.Game.GridFight.Sync;
using CastoriceHoney.Server.GameServer.Server.Packet.Send.GridFight;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightUpdateTraitTrackCsReq)]
public class HandlerGridFightUpdateTraitTrackCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightUpdateTraitTrackCsReq.Parser.ParseFrom(data);

        var inst = connection.Player!.GridFightManager!.GridFightInstance;
        if (inst == null)
        {
            await connection.SendPacket(CmdIds.GridFightUpdateTraitTrackScRsp);
            return;
        }

        var component = inst.GetComponent<GridFightBasicComponent>();
        if (req.IsTracking)
        {
            component.Data.TrackingTraits.Add(req.TraitId);
        }
        else
        {
            component.Data.TrackingTraits.Remove(req.TraitId);
        }

        // sync
        await connection.SendPacket(
            new PacketGridFightSyncUpdateResultScNotify(
                new GridFightTraitTrackSyncData(GridFightSrc.KGridFightSrcNone, component.Data)));

        await connection.SendPacket(CmdIds.GridFightUpdateTraitTrackScRsp);
    }
}