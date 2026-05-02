using CastoriceHoney.Server.GameServer.Server.Packet.Send.Rogue;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Rogue;

[Opcode(CmdIds.EnhanceRogueBuffCsReq)]
public class HandlerEnhanceRogueBuffCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = EnhanceRogueBuffCsReq.Parser.ParseFrom(data);

        var rogue = connection.Player!.RogueManager?.GetRogueInstance();
        if (rogue == null) return;

        await rogue.EnhanceBuff((int)req.MazeBuffId, RogueCommonActionResultSourceType.Enhance);

        await connection.SendPacket(new PacketEnhanceRogueBuffScRsp(req.MazeBuffId));
    }
}