using CastoriceHoney.Server.GameServer.Server.Packet.Send.Battle;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Battle;

[Opcode(CmdIds.GetFarmStageGachaInfoCsReq)]
public class HandlerGetFarmStageGachaInfoCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GetFarmStageGachaInfoCsReq.Parser.ParseFrom(data);
        await connection.SendPacket(new PacketGetFarmStageGachaInfoScRsp(req));
    }
}