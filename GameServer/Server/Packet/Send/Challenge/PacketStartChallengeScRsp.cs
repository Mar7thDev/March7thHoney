using CastoriceHoney.Server.GameServer.Game.Challenge.Definitions;
using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Challenge;

public class PacketStartChallengeScRsp : BasePacket
{
    public PacketStartChallengeScRsp(uint Retcode) : base(CmdIds.StartChallengeScRsp)
    {
        var proto = new StartChallengeScRsp
        {
            Retcode = Retcode
        };

        SetData(proto);
    }

    public PacketStartChallengeScRsp(PlayerInstance player, bool sendScene = true) : base(CmdIds.StartChallengeScRsp)
    {
        StartChallengeScRsp proto = new();

        if (player.ChallengeManager!.ChallengeInstance != null)
        {
            if (player.ChallengeManager.ChallengeInstance is BaseLegacyChallengeInstance inst)
            {
                proto.CurChallenge = inst.ToProto();
                proto.StageInfo = inst.ToStageInfo();
            }

            proto.LineupList.Add(player.LineupManager!.GetExtraLineup(ExtraLineupType.LineupChallenge)!.ToProto());
            proto.LineupList.Add(player.LineupManager!.GetExtraLineup(ExtraLineupType.LineupChallenge2)!.ToProto());
            if (sendScene) proto.Scene = player.SceneInstance!.ToProto();
        }
        else
        {
            proto.Retcode = 1;
        }

        SetData(proto);
    }
}