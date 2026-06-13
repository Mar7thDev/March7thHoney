using March7thHoney.GameServer.Server.Packet.Send.JukeBox;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.JukeBox;

[Opcode(CmdIds.PlayBackGroundMusicCsReq)]
public class HandlerPlayBackGroundMusicCsReq : Handler<PlayBackGroundMusicCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, PlayBackGroundMusicCsReq req)
    {
        // 4.3: DMGCIDGKPFF -> IKJNIKIGFLF, LEKGBMCOJDB -> JKNLCEEDAHJ
        var musicId = req.IKJNIKIGFLF?.JKNLCEEDAHJ?.Id ?? 0;

        player.Data.CurrentBgm = (int)musicId;

        await connection.SendPacket(new PacketPlayBackGroundMusicScRsp(musicId));
    }
}

