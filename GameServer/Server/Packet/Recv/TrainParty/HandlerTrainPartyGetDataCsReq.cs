using CastoriceHoney.Server.GameServer.Server.Packet.Send.TrainParty;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.TrainParty;

[Opcode(CmdIds.TrainPartyGetDataCsReq)]
public class HandlerTrainPartyGetDataCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketTrainPartyGetDataScRsp(connection.Player!));
    }
}