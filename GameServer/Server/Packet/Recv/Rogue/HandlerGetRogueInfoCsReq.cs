using March7thHoney.GameServer.Server.Packet.Send.Rogue;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.Rogue;

[Opcode(CmdIds.GetRogueInfoCsReq)]
public class HandlerGetRogueInfoCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketGetRogueInfoScRsp(connection.Player!));
    }
}