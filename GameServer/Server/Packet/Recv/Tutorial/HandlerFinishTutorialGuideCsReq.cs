using March7thHoney.GameServer.Server.Packet.Send.Tutorial;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Tutorial;

[Opcode(CmdIds.FinishTutorialGuideCsReq)]
public class HandlerFinishTutorialGuideCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = FinishTutorialGuideCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;
        player.TutorialGuideData!.Tutorials[(int)req.GroupId] = TutorialStatus.TutorialFinish;

        await connection.SendPacket(new PacketFinishTutorialGuideScRsp(req.GroupId, req.Type));
    }
}
