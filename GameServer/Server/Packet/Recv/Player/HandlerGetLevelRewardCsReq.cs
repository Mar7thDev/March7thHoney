using March7thHoney.Data;
using March7thHoney.GameServer.Server.Packet.Send.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Player;

[Opcode(CmdIds.GetLevelRewardCsReq)]
public class HandlerGetLevelRewardCsReq : Handler<JDAFEAJCJIB>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, JDAFEAJCJIB req)
    {
        if (player.Data.TakenLevelReward.Contains((int)req.AOEBNCAILPJ))
        {
            await connection.SendPacket(new PacketGetLevelRewardScRsp(Retcode.RetLevelRewardHasTaken));
            return;
        }

        if (player.Data.Level < req.AOEBNCAILPJ)
        {
            await connection.SendPacket(new PacketGetLevelRewardScRsp(Retcode.RetLevelRewardLevelError));
            return;
        }

        if (!GameData.PlayerLevelConfigData.TryGetValue((int)req.AOEBNCAILPJ, out var levelExcel))
        {
            await connection.SendPacket(new PacketGetLevelRewardScRsp(Retcode.RetLevelRewardLevelError));
            return;
        }

        player.Data.TakenLevelReward.Add((int)req.AOEBNCAILPJ);
        var rewards = await player.InventoryManager!.HandleReward(levelExcel.LevelRewardID);
        await connection.SendPacket(new PacketGetLevelRewardScRsp(req.AOEBNCAILPJ, rewards));
    }
}
