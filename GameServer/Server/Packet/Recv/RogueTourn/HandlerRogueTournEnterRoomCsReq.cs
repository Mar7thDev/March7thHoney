using March7thHoney.Enums.TournRogue;
using March7thHoney.GameServer.Server.Packet.Send.RogueTourn;
using March7thHoney.GameServer.Server.Packet.Send.Scene;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.RogueTourn;

[Opcode(CmdIds.RogueTournEnterRoomCsReq)]
public class HandlerRogueTournEnterRoomCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = RogueTournEnterRoomCsReq.Parser.ParseFrom(data);

        var player = connection.Player!;

        var inst = player.RogueTournManager?.RogueTournInstance;
        if (inst == null)
        {
            await connection.SendPacket(new PacketRogueTournEnterRoomScRsp(Retcode.RetRogueStatusNotMatch, null));
            return;
        }

        await inst.EnterRoom((int)(req.CurRoomIndex + 1), (RogueTournRoomTypeEnum)req.NextRoomType);
        await connection.SendPacket(new PacketRogueTournEnterRoomScRsp(Retcode.RetSucc, inst));
        await connection.SendPacket(new PacketEnterSceneByServerScNotify(player.SceneInstance!));
    }
}