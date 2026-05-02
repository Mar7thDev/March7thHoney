using CastoriceHoney.Server.GameServer.Server.Packet.Send.Pet;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Pet;

[Opcode(CmdIds.RecallPetCsReq)]
public class HandlerRecallPetCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = RecallPetCsReq.Parser.ParseFrom(data);

        connection.Player!.Data.Pet = 0;

        await connection.SendPacket(new PacketRecallPetScRsp(req.SummonedPetId));
    }
}