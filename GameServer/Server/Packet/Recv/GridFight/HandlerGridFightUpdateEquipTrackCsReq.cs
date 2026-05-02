using CastoriceHoney.Server.GameServer.Game.GridFight.Component;
using CastoriceHoney.Server.GameServer.Game.GridFight.Sync;
using CastoriceHoney.Server.GameServer.Server.Packet.Send.GridFight;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;
using CastoriceHoney.Server.Proto.ServerSide;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightUpdateEquipTrackCsReq)]
public class HandlerGridFightUpdateEquipTrackCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightUpdateEquipTrackCsReq.Parser.ParseFrom(data);

        var inst = connection.Player!.GridFightManager!.GridFightInstance;
        if (inst == null)
        {
            await connection.SendPacket(CmdIds.GridFightUpdateEquipTrackScRsp);
            return;
        }

        var component = inst.GetComponent<GridFightBasicComponent>();
        foreach (var info in req.TrackInfoList)
        {
            var target = component.Data.TrackingEquipments.FirstOrDefault(x => x.RoleId == info.TrackRoleId);
            if (info.IsTracking)
            {
                if (target == null)
                {
                    target = new GridFightEquipmentTrackInfoPb
                    {
                        RoleId = info.TrackRoleId,
                        Priority = info.TrackPriority,
                    };
                    target.EquipmentIds.AddRange(info.GridFightItemList);

                    component.Data.TrackingEquipments.Add(target);
                }
                else
                {
                    target.Priority = info.TrackPriority;
                    target.EquipmentIds.Clear();
                    target.EquipmentIds.AddRange(info.GridFightItemList);
                }
            }
            else
            {
                if (target != null)
                {
                    component.Data.TrackingEquipments.Remove(target);
                }
            }
        }

        // sync
        await connection.SendPacket(
            new PacketGridFightSyncUpdateResultScNotify(
                new GridFightRoleTrackEquipmentSyncData(GridFightSrc.KGridFightSrcNone, component)));

        await connection.SendPacket(CmdIds.GridFightUpdateEquipTrackScRsp);
    }
}