using CastoriceHoney.Server.GameServer.Server.Packet.Send.Gacha;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Gacha;

[Opcode(CmdIds.DoGachaCsReq)]
public class HandlerDoGachaCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = DoGachaCsReq.Parser.ParseFrom(data);
        var gain = await connection.Player!.GachaManager!.DoGacha((int)req.GachaId, (int)req.GachaNum);

        if (gain != null)
            await connection.SendPacket(new PacketDoGachaScRsp(gain));
        else
            await connection.SendPacket(new PacketDoGachaScRsp());
    }
}