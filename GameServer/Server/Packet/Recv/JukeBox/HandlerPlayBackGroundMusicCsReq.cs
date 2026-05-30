using March7thHoney.GameServer.Server.Packet.Send.JukeBox;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.JukeBox;

[Opcode(CmdIds.PlayBackGroundMusicCsReq)]
public class HandlerPlayBackGroundMusicCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = PlayBackGroundMusicCsReq.Parser.ParseFrom(data);
        // 4.3: DMGCIDGKPFF -> IKJNIKIGFLF, LEKGBMCOJDB -> JKNLCEEDAHJ
        var musicId = req.IKJNIKIGFLF?.JKNLCEEDAHJ?.Id ?? 0;

        connection.Player!.Data.CurrentBgm = (int)musicId;

        await connection.SendPacket(new PacketPlayBackGroundMusicScRsp(musicId));
    }
}

