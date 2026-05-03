using March7thHoney.Data;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.ChallengePeak;

public class PacketGetChallengePeakDataScRsp : BasePacket
{
    public PacketGetChallengePeakDataScRsp(PlayerInstance player) : base(CmdIds.GetChallengePeakDataScRsp)
    {
        var proto = new GetChallengePeakDataScRsp
        {
            CurrentPeakGroupId = (uint)GameData.GetCurrentChallengePeakGroupId()
        };

        foreach (var groupId in GameData.ChallengePeakGroupConfigData.Keys)
            proto.ChallengePeakGroups.Add(player.ChallengePeakManager!.GetChallengePeakInfo(groupId));

        SetData(proto);
    }
}
