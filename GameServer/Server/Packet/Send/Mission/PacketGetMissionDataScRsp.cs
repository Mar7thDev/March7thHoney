using March7thHoney.Data;
using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Mission;

public class PacketGetMissionDataScRsp : BasePacket
{
    public PacketGetMissionDataScRsp(PlayerInstance player) : base(CmdIds.GetMissionDataScRsp)
    {
        var proto = new GetMissionDataScRsp
        {
            TrackMissionId = (uint)player.MissionManager!.GetClientTrackingMainMissionId()
        };

        foreach (var mission in GameData.MainMissionData.Keys)
            if (player.MissionManager!.GetMainMissionStatus(mission) == MissionPhaseEnum.Accept &&
                player.MissionManager.IsMainMissionVisible(mission))
                proto.MainMissionList.Add(new MainMission
                {
                    Id = (uint)mission,
                    Status = MissionStatus.MissionDoing
                });

        foreach (var mission in GameData.SubMissionInfoData.Keys.Concat(GameData.SubMissionData.Keys).Distinct())
            if (player.MissionManager!.GetSubMissionStatus(mission) == MissionPhaseEnum.Accept &&
                player.MissionManager.IsMainMissionVisible(
                    GameData.SubMissionInfoData.GetValueOrDefault(mission)?.MainMissionId ??
                    GameData.SubMissionData.GetValueOrDefault(mission)?.MainMissionID ?? 0))
                proto.MissionList.Add(new Proto.Mission
                {
                    Id = (uint)mission,
                    Status = MissionStatus.MissionDoing,
                    Progress = (uint)player.MissionManager!.GetMissionProgress(mission)
                });

        SetData(proto);
    }
}
