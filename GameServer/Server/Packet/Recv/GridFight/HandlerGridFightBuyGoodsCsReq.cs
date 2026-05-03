using March7thHoney.GameServer.Server.Packet.Send.GridFight;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightBuyGoodsCsReq)]
public class HandlerGridFightBuyGoodsCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        _ = GridFightBuyGoodsCsReq.Parser.ParseFrom(data);
        var rsp = new BasePacket((ushort)CmdIds.CEFIMADBIBH);
        rsp.SetData(new CEFIMADBIBH());
        await connection.SendPacket(rsp);
        await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(connection.Player!));
    }
}
