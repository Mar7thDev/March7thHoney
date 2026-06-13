using March7thHoney.GameServer.Server.Packet.Send.ContentPackage;
using March7thHoney.GameServer.Server.Packet.Send.Player;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.Player;

[Opcode(CmdIds.PlayerLoginCsReq)]
public class HandlerPlayerLoginCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        connection.State = SessionStateEnum.ACTIVE;
        await player.OnLogin();
        await connection.SendPacket(new PacketPlayerLoginScRsp(connection));
        await connection.SendPacket(new PacketContentPackageSyncDataScNotify());
    }
}
