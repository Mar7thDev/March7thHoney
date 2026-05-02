using CastoriceHoney.Server.GameServer.Server.Packet.Send.JukeBox;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.JukeBox;

[Opcode(CmdIds.PlayBackGroundMusicCsReq)]
public class HandlerPlayBackGroundMusicCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = PlayBackGroundMusicCsReq.Parser.ParseFrom(data);
        var musicId = req.DMGCIDGKPFF?.LEKGBMCOJDB?.Id ?? 0;

        connection.Player!.Data.CurrentBgm = (int)musicId;

        await connection.SendPacket(new PacketPlayBackGroundMusicScRsp(musicId));
    }
}

