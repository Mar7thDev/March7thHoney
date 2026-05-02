using CastoriceHoney.Server.GameServer.Server.Packet.Send.Gacha;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Gacha;

[Opcode(CmdIds.SetGachaDecideItemCsReq)]
public class HandlerSetGachaDecideItemCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = SetGachaDecideItemCsReq.Parser.ParseFrom(data);

        connection.Player!.GachaManager!.GachaData.GachaDecideOrder = req.DGOMHDMJHEK.Select(x => (int)x).ToList();

        await connection.SendPacket(new PacketSetGachaDecideItemScRsp(req.GachaId, req.DGOMHDMJHEK.ToList()));
    }
}

