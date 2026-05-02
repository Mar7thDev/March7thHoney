using CastoriceHoney.Server.Enums.TournRogue;
using CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueTourn;
using CastoriceHoney.Server.GameServer.Server.Packet.Send.Scene;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.RogueTourn;

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