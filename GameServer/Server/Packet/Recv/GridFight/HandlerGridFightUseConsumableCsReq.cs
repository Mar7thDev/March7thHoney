using CastoriceHoney.Server.GameServer.Game.GridFight.Component;
using CastoriceHoney.Server.GameServer.Server.Packet.Send.GridFight;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightUseConsumableCsReq)]
public class HandlerGridFightUseConsumableCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightUseConsumableCsReq.Parser.ParseFrom(data);

        var gridFight = connection.Player?.GridFightManager?.GridFightInstance;
        if (gridFight == null)
        {
            await connection.SendPacket(new PacketGridFightUseConsumableScRsp(Retcode.RetGridFightNotInGameplay));
            return;
        }

        var itemsComp = gridFight.GetComponent<GridFightItemsComponent>();

        var code = await itemsComp.UseConsumable(req.ItemId, req.DisplayValue);
        await connection.SendPacket(new PacketGridFightUseConsumableScRsp(code));
    }
}