using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Rogue;

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