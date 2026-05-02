using CastoriceHoney.Server.GameServer.Game.Challenge.Instances;
using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ChallengePeak;

public class PacketGetCurChallengePeakScRsp : BasePacket
{
    public PacketGetCurChallengePeakScRsp(PlayerInstance player) : base(CmdIds.GetCurChallengePeakScRsp)
    {
        var proto = new GetCurChallengePeakScRsp();

        if (player.ChallengeManager!.ChallengeInstance is ChallengePeakInstance peak)
        {
            proto.IsFinished = true;
            proto.PeakLevelId = peak.Data.Peak.CurrentPeakLevelId;
            proto.PeakBossBuff = peak.Data.Peak.Buffs.FirstOrDefault(0u);
            proto.PeakRoundsCount = peak.Data.Peak.RoundCnt;
        }

        SetData(proto);
    }
}