using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.Mission;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Mission;

public class PacketUpdateMainMissionCustomValueScRsp : BasePacket
{
    public PacketUpdateMainMissionCustomValueScRsp(Retcode retcode, PlayerInstance player, uint mainMissionId) : base(
        CmdIds.UpdateMainMissionCustomValueScRsp)
    {
        var proto = new UpdateMainMissionCustomValueScRsp { Retcode = (uint)retcode };

        // MainMission is the response's only payload field: the official reply echoes the main's full record.
        if (retcode == Retcode.RetSucc)
        {
            var main = new MainMission
            {
                Id = mainMissionId,
                Status = player.MissionManager!.GetMainMissionStatus((int)mainMissionId).ToProto()
            };
            MissionSyncBuilder.FillMainCustomValues(main,
                player.MissionManager.GetMainMissionCustomValues((int)mainMissionId));
            proto.HKDDMGGDCIP = main;
        }

        SetData(proto);
    }
}
