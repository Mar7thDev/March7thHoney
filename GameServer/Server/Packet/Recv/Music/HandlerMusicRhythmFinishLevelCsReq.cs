using CastoriceHoney.Server.GameServer.Server.Packet.Send.Music;
using CastoriceHoney.Server.Kcp;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Music;

[Opcode(CmdIds.MusicRhythmFinishLevelCsReq)]
public class HandlerMusicRhythmFinishLevelCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var curLevel = connection.Player!.Data.CurMusicLevel;
        await connection.SendPacket(new PacketMusicRhythmFinishLevelScRsp((uint)curLevel));
    }
}