using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Mail;

public class PacketGetMailScRsp : BasePacket
{
    public PacketGetMailScRsp(PlayerInstance player) : base(CmdIds.GetMailScRsp)
    {
        var list = player.MailManager!.ToMailProto();
        var noticeList = player.MailManager!.ToNoticeMailProto();
        var proto = new GetMailScRsp
        {
            IsEnd = true,
            MailList = { list },
            NoticeMailList = { noticeList },
            TotalNum = (uint)(list.Count + noticeList.Count)
        };

        SetData(proto);
    }
}