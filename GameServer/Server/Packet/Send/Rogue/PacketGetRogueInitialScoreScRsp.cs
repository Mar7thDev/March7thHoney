using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Rogue;

public class PacketGetRogueInitialScoreScRsp : BasePacket
{
    public PacketGetRogueInitialScoreScRsp(PlayerInstance player) : base(CmdIds.GetRogueInitialScoreScRsp)
    {
        var proto = new GetRogueInitialScoreScRsp
        {
            RogueScoreRewardInfo = player.RogueManager!.ToRewardProto()
        };

        SetData(proto);
    }
}