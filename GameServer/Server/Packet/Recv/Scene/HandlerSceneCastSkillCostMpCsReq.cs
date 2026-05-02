using CastoriceHoney.Server.GameServer.Server.Packet.Send.Scene;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Scene;

[Opcode(CmdIds.SceneCastSkillCostMpCsReq)]
public class HandlerSceneCastSkillCostMpCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = SceneCastSkillCostMpCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;
        await player.LineupManager!.CostMp(1, req.CastEntityId);
        await connection.SendPacket(new PacketSceneCastSkillCostMpScRsp((int)req.CastEntityId));
    }
}