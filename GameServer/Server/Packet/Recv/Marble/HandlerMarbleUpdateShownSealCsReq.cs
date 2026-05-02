using March7thHoney.GameServer.Server.Packet.Send.Marble;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Marble;

[Opcode(CmdIds.MarbleUpdateShownSealCsReq)]
public class HandlerMarbleUpdateShownSealCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = MarbleUpdateShownSealCsReq.Parser.ParseFrom(data);

        await connection.SendPacket(new PacketMarbleUpdateShownSealScRsp(req.UpdateSealList));
    }
}