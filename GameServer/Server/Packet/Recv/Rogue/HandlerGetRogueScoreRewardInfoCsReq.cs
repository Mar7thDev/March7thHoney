using CastoriceHoney.Server.GameServer.Server.Packet.Send.Rogue;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Rogue;

[Opcode(CmdIds.GetRogueScoreRewardInfoCsReq)]
public class HandlerGetRogueScoreRewardInfoCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketGetRogueScoreRewardInfoScRsp(connection.Player!));
    }
}