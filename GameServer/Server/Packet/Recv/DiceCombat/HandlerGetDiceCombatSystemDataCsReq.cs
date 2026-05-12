using March7thHoney.GameServer.Server.Packet.Send.DiceCombat;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.DiceCombat;

[Opcode(CmdIds.GetDiceCombatSystemDataCsReq)]
public class HandlerGetDiceCombatSystemDataCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var player = connection.Player!;
        await connection.SendPacket(new PacketGetDiceCombatSystemDataScRsp(player));
    }
}
