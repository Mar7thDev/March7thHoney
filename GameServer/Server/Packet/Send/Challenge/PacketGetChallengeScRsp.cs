using CastoriceHoney.Server.Data;
using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Challenge;

public class PacketGetChallengeScRsp : BasePacket
{
    public PacketGetChallengeScRsp(PlayerInstance player) : base(CmdIds.GetChallengeScRsp)
    {
        var proto = new GetChallengeScRsp
        {
            Retcode = 0
        };

        foreach (var challengeExcel in GameData.ChallengeConfigData.Values)
            if (player.ChallengeManager?.ChallengeData.History.TryGetValue(challengeExcel.ID, out var value) == true)
            {
                var history = value;
                proto.ChallengeList.Add(history.ToProto());
            }
            else
            {
                proto.ChallengeList.Add(new Proto.Challenge
                {
                    ChallengeId = (uint)challengeExcel.ID
                });
            }

        foreach (var reward in player.ChallengeManager?.ChallengeData?.TakenRewards.Values.ToList() ?? [])
            proto.ChallengeGroupList.Add(reward.ToProto());

        SetData(proto);
    }
}