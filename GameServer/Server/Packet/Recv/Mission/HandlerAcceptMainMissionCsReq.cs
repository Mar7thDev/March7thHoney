using CastoriceHoney.Server.GameServer.Server.Packet.Send.Mission;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Mission;

[Opcode(CmdIds.AcceptMainMissionCsReq)]
public class HandlerAcceptMainMissionCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = AcceptMainMissionCsReq.Parser.ParseFrom(data);
        var missionId = req.MainMissionId;

        await connection.Player!.MissionManager!.AcceptMainMission((int)missionId);

        await connection.SendPacket(new PacketAcceptMainMissionScRsp(missionId));
    }
}