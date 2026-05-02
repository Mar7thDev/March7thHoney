using March7thHoney.GameServer.Server.Packet.Send.Fight;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Fight;

[Opcode(CmdIds.FightHeartBeatCsReq)]
public class HandlerFightHeartBeatCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = FightHeartBeatCsReq.Parser.ParseFrom(data);

        if (connection.MarbleRoom != null) await connection.MarbleRoom.OnPlayerHeartBeat();

        await connection.SendPacket(new PacketFightHeartBeatScRsp(req.ClientTimeMs));
    }
}