using March7thHoney.GameServer.Server.Packet.Send.RogueTourn;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.RogueTourn;

[Opcode(CmdIds.RogueTournGetPermanentTalentInfoCsReq)]
public class HandlerRogueTournGetPermanentTalentInfoCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketRogueTournGetPermanentTalentInfoScRsp(connection.Player!));
    }
}