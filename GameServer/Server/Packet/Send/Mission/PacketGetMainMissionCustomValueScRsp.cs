using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.Mission;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Mission;

public class PacketGetMainMissionCustomValueScRsp : BasePacket
{
    public PacketGetMainMissionCustomValueScRsp(GetMainMissionCustomValueCsReq req, PlayerInstance player) : base(
        CmdIds.GetMainMissionCustomValueScRsp)
    {
        var proto = new GetMainMissionCustomValueScRsp();
        foreach (var mission in req.MainMissionIdList)
        {
            var main = new MainMission
            {
                Id = mission,
                Status = player.MissionManager!.GetMainMissionStatus((int)mission).ToProto()
            };
            // The client re-reads its act choices from here after a relog; without them an act that branches
            // on ByCompareMissionCustomValue takes neither branch.
            MissionSyncBuilder.FillMainCustomValues(main,
                player.MissionManager.GetMainMissionCustomValues((int)mission));
            proto.MainMissionList.Add(main);
        }

        SetData(proto);
    }
}
