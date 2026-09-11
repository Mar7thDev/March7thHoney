using March7thHoney.GameServer.Server.Packet.Send.FiveDim;
using March7thHoney.Kcp;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Server.Packet.Recv.FiveDim;

[Opcode(CmdIds.GetFiveDimGameDataCsReq)]
public class HandlerGetFiveDimGameDataCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        var packet = new PacketGetFiveDimGameDataScRsp(player);
        Logger.GetByClassName().Debug($"FiveDim response: cmd={packet.CmdId}, payload={Convert.ToHexString(packet.Data)}");
        await connection.SendPacket(packet);
    }
}
