using March7thHoney.GameServer.Game.GridFight.Component;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightEquipCraftCsReq)]
public class HandlerGridFightEquipCraftCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightEquipCraftCsReq.Parser.ParseFrom(data);

        var inst = connection.Player!.GridFightManager!.GridFightInstance;
        if (inst == null)
        {
            await connection.SendPacket(CmdIds.GridFightEquipCraftScRsp);
            return;
        }

        var component = inst.GetComponent<GridFightItemsComponent>();
        await component.CraftEquipment(req.CraftEquipId, req.CraftMaterials.ToList());

        await connection.SendPacket(CmdIds.GridFightEquipCraftScRsp);
    }
}