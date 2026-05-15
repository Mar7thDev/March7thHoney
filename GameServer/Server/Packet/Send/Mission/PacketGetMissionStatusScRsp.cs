using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;
using System.Linq;

namespace March7thHoney.GameServer.Server.Packet.Send.Mission;

public class PacketGetMissionStatusScRsp : BasePacket
{
    public PacketGetMissionStatusScRsp(GetMissionStatusCsReq req, PlayerInstance player) : base(
        CmdIds.GetMissionStatusScRsp)
    {
        var proto = new GetMissionStatusScRsp();

        foreach (var item in req.MainMissionIdList.Distinct())
        {
            var status = player.MissionManager!.GetMainMissionStatus((int)item);
            if (status == MissionPhaseEnum.Finish)
            {
                proto.FinishedMainMissionIdList.Add(item);
                proto.CurversionFinishedMainMissionIdList.Add(item);
            }
            else if (status == MissionPhaseEnum.Accept)
            {
                proto.UnfinishedMainMissionIdList.Add(item);
            }

            var mainMissionCustomValue = new MainMissionCustomValue
            {
                MainMissionId = item,
                CustomValueList = new EPNCDFBAHEA()
            };
            mainMissionCustomValue.CustomValueList.CustomValueList.Add(
                player.MissionManager.GetMainMissionCustomValueList((int)item));
            if (mainMissionCustomValue.CustomValueList.CustomValueList.Count > 0)
                proto.MainMissionMcvList.Add(mainMissionCustomValue);
        }

        foreach (var item in req.SubMissionIdList.Distinct())
        {
            var status = player.MissionManager!.GetSubMissionStatus((int)item);
            proto.SubMissionStatusList.Add(new Proto.Mission
            {
                Id = item,
                Status = status.ToProto()
            });
        }

        SetData(proto);
    }
}
