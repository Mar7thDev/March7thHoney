using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Phone;

public class PacketSelectPhoneThemeScRsp : BasePacket
{
    public PacketSelectPhoneThemeScRsp(uint themeId) : base(CmdIds.SelectPhoneThemeScRsp)
    {
        var proto = new SelectPhoneThemeScRsp
        {
            CurPhoneTheme = themeId
        };

        SetData(proto);
    }
}