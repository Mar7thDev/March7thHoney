using March7thHoney.GameServer.Game.Friend;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Friend;

public class PacketGetAssistHistoryScRsp : BasePacket
{
    public PacketGetAssistHistoryScRsp(PlayerInstance player) : base(CmdIds.GetAssistHistoryScRsp)
    {
        var data = AssistRewardService.Get(player.Uid);
        var proto = new GetAssistHistoryScRsp
        {
            LLIPLPNDIMC = data.TotalAssists,
            OFEKHCHHOFD = data.PendingUnits
        };

        SetData(proto);
    }
}
