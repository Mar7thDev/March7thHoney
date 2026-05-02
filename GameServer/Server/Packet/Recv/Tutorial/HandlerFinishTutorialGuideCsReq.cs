using CastoriceHoney.Server.GameServer.Server.Packet.Send.Tutorial;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Tutorial;

[Opcode(CmdIds.FinishTutorialGuideCsReq)]
public class HandlerFinishTutorialGuideCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = FinishTutorialGuideCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;
        if (player.TutorialGuideData!.Tutorials.TryGetValue((int)req.GroupId, out var res))
            if (res != TutorialStatus.TutorialFinish)
            {
                await player.InventoryManager!.AddItem(1, 1, false);
                player.TutorialGuideData!.Tutorials[(int)req.GroupId] = TutorialStatus.TutorialFinish;
            }

        await connection.SendPacket(new PacketFinishTutorialGuideScRsp(req.GroupId));
    }
}