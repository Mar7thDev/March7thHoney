using March7thHoney.GameServer.Server.Packet.Send.Tutorial;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Tutorial;

[Opcode(CmdIds.UnlockTutorialGuideCsReq)]
public class HandlerUnlockTutorialGuideCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = UnlockTutorialGuideCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;
        player.TutorialGuideData!.Tutorials[(int)req.GroupId] = TutorialStatus.TutorialUnlock;

        await connection.SendPacket(new PacketUnlockTutorialGuideScRsp(req.GroupId, req.Type));
    }
}
