using CastoriceHoney.Server.Enums.Mission;
using CastoriceHoney.Server.GameServer.Server.Packet.Send.HeartDial;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.HeartDial;

[Opcode(CmdIds.FinishEmotionDialoguePerformanceCsReq)]
public class HandlerFinishEmotionDialoguePerformanceCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = FinishEmotionDialoguePerformanceCsReq.Parser.ParseFrom(data);

        var player = connection.Player!;
        await player.MissionManager!.HandleFinishType(MissionFinishTypeEnum.HeartDialDialoguePerformanceFinish,
            $"HeartDial_{req.DialogueId}");

        await connection.SendPacket(new PacketFinishEmotionDialoguePerformanceScRsp(req.ScriptId, req.DialogueId));
    }
}