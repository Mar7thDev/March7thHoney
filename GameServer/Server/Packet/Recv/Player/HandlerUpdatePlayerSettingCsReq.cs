using CastoriceHoney.Server.GameServer.Server.Packet.Send.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Player;

[Opcode(CmdIds.UpdatePlayerSettingCsReq)]
public class HandlerUpdatePlayerSettingCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = UpdatePlayerSettingCsReq.Parser.ParseFrom(data);
        await connection.SendPacket(new PacketUpdatePlayerSettingScRsp(req.PlayerSetting));
    }
}