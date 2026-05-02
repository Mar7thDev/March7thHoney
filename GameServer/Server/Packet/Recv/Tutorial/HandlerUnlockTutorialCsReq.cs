using CastoriceHoney.Server.GameServer.Server.Packet.Send.Tutorial;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Tutorial;

[Opcode(CmdIds.UnlockTutorialCsReq)]
public class HandlerUnlockTutorialCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = UnlockTutorialCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;
        if (!player.TutorialData!.Tutorials.TryGetValue((int)req.TutorialId, out _))
            player.TutorialData!.Tutorials.Add((int)req.TutorialId, TutorialStatus.TutorialUnlock);

        await connection.SendPacket(new PacketUnlockTutorialScRsp(req.TutorialId));
    }
}