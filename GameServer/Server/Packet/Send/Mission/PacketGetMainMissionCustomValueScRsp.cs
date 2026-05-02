using CastoriceHoney.Server.Enums.Mission;
using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Mission;

public class PacketGetMainMissionCustomValueScRsp : BasePacket
{
    public PacketGetMainMissionCustomValueScRsp(GetMainMissionCustomValueCsReq req, PlayerInstance player) : base(
        CmdIds.GetMainMissionCustomValueScRsp)
    {
        var proto = new GetMainMissionCustomValueScRsp();
        foreach (var mission in req.MainMissionIdList)
            proto.MainMissionList.Add(new MainMission
            {
                Id = mission,
                Status = player.MissionManager!.GetMainMissionStatus((int)mission).ToProto()
            });

        SetData(proto);
    }
}