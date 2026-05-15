using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;
using System.Linq;

namespace March7thHoney.GameServer.Server.Packet.Send.Mission;

public class PacketGetMainMissionCustomValueScRsp : BasePacket
{
    public PacketGetMainMissionCustomValueScRsp(GetMainMissionCustomValueCsReq req, PlayerInstance player) : base(
        CmdIds.GetMainMissionCustomValueScRsp)
    {
        var proto = new GetMainMissionCustomValueScRsp();
        foreach (var mission in req.MainMissionIdList.Distinct())
        {
            var mainMission = new MainMission
            {
                Id = mission,
                Status = player.MissionManager!.GetMainMissionStatus((int)mission).ToProto()
            };

            mainMission.CustomValueList.Add(player.MissionManager.GetMainMissionCustomValueList((int)mission));

            proto.MainMissionList.Add(mainMission);
        }

        SetData(proto);
    }
}
