using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Rogue;

public class PacketGetRogueScoreRewardInfoScRsp : BasePacket
{
    public PacketGetRogueScoreRewardInfoScRsp(PlayerInstance player) : base(CmdIds.GetRogueScoreRewardInfoScRsp)
    {
        var proto = new GetRogueScoreRewardInfoScRsp
        {
            Info = player.RogueManager!.ToRewardProto()
        };

        SetData(proto);
    }
}