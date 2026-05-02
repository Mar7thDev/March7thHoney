using March7thHoney.GameServer.Game.GridFight.Component;
using March7thHoney.GameServer.Game.GridFight.Sync;
using March7thHoney.GameServer.Server.Packet.Send.GridFight;
using March7thHoney.Kcp;
using March7thHoney.Proto;
using March7thHoney.Proto.ServerSide;

namespace March7thHoney.GameServer.Server.Packet.Recv.GridFight;

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