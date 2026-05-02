using CastoriceHoney.Server.Data;
using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ChallengePeak;

public class PacketGetChallengePeakDataScRsp : BasePacket
{
    public PacketGetChallengePeakDataScRsp(PlayerInstance player) : base(CmdIds.GetChallengePeakDataScRsp)
    {
        var proto = new GetChallengePeakDataScRsp
        {
            CurPeakGroupId = 1
        };

        foreach (var groupId in GameData.ChallengePeakGroupConfigData.Keys)
            proto.ChallengePeakLevelList.Add(player.ChallengePeakManager!.GetChallengePeakInfo(groupId));

        SetData(proto);
    }
}