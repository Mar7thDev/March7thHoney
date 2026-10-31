using March7thHoney.GameServer.Game.Friend;
using March7thHoney.GameServer.Server.Packet.Send.Friend;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.Friend;

[Opcode(CmdIds.TakeAssistRewardCsReq)]
public class HandlerTakeAssistRewardCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        var (rows, items) = await AssistRewardService.Claim(player);
        await connection.SendPacket(new PacketTakeAssistRewardScRsp(rows, items));
    }
}
