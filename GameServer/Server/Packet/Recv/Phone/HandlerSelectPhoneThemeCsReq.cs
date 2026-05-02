using CastoriceHoney.Server.GameServer.Server.Packet.Send.Phone;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Phone;

[Opcode(CmdIds.SelectPhoneThemeCsReq)]
public class HandlerSelectPhoneThemeCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = SelectPhoneThemeCsReq.Parser.ParseFrom(data);

        connection.Player!.Data.PhoneTheme = (int)req.ThemeId;

        await connection.SendPacket(new PacketSelectPhoneThemeScRsp(req.ThemeId));
    }
}