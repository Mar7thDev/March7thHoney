using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Rogue;

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