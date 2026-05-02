using March7thHoney.GameServer.Server.Packet.Send.MapRotation;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.MapRotation;

[Opcode(CmdIds.DeployRotaterCsReq)]
public class HandlerDeployRotaterCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = DeployRotaterCsReq.Parser.ParseFrom(data);

        connection.Player!.ChargerNum--;
        await connection.SendPacket(new PacketDeployRotaterScRsp(req.RotaterData, connection.Player!.ChargerNum, 5));
    }
}
