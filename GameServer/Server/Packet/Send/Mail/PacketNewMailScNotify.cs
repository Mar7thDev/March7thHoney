using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Mail;

public class PacketNewMailScNotify : BasePacket
{
    public PacketNewMailScNotify(int id) : base(CmdIds.NewMailScNotify)
    {
        var proto = new NewMailScNotify
        {
            MailIdList = { (uint)id }
        };

        SetData(proto);
    }
}