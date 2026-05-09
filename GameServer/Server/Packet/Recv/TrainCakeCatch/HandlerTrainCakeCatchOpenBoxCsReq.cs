using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.TrainCakeCatch;

[Opcode(CmdIds.TrainCakeCatchOpenBoxCsReq)]
public class HandlerTrainCakeCatchOpenBoxCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var packet = new BasePacket(CmdIds.TrainCakeCatchOpenBoxScRsp);
        packet.SetData(new TrainCakeCatchOpenBoxScRsp());
        await connection.SendPacket(packet);
    }
}
