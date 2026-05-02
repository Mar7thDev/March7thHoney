using March7thHoney.Enums.TournRogue;
using March7thHoney.GameServer.Server.Packet.Send.RogueTourn;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.RogueTourn;

[Opcode(CmdIds.RogueTournEnterLayerCsReq)]
public class HandlerRogueTournEnterLayerCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = RogueTournEnterLayerCsReq.Parser.ParseFrom(data);
        var inst = connection.Player!.RogueTournManager?.RogueTournInstance;

        if (inst == null)
        {
            await connection.SendPacket(new PacketRogueTournEnterLayerScRsp(Retcode.RetRogueStatusNotMatch, null));
            return;
        }

        await inst.EnterNextLayer((int)req.CurLevelIndex, (RogueTournRoomTypeEnum)req.NextRoomType);
        await connection.SendPacket(new PacketRogueTournEnterLayerScRsp(Retcode.RetSucc, inst));
    }
}