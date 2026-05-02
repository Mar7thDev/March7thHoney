using CastoriceHoney.Server.GameServer.Server.Packet.Send.Battle;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Battle;

[Opcode(CmdIds.GetCurBattleInfoCsReq)]
public class HandlerGetCurBattleInfoCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketGetCurBattleInfoScRsp());
    }
}