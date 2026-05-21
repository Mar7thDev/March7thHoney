using March7thHoney.Kcp;
using March7thHoney.Proto;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Server.Packet.Send.Player;

public class PacketServerAnnounceNotify : BasePacket
{
    public PacketServerAnnounceNotify(string? customContent = null) : base(CmdIds.ServerAnnounceNotify)
    {
        var content = string.IsNullOrWhiteSpace(customContent)
            ? ConfigManager.Config.ServerOption.ServerAnnounce.AnnounceContent
            : customContent;
        if (string.IsNullOrWhiteSpace(content)) return;

        var proto = new ServerAnnounceNotify();
        var now = Extensions.GetUnixSec();

        proto.AnnounceDataList.Add(new AnnounceData
        {
            BeginTime = now,
            EndTime = now + 86400 * 30,
            ConfigId = 1,
            BannerFrequency = 3600,
            HasRecommand = true,
            EmergencyText = content,
            BannerText = content
        });

        SetData(proto);
    }
}
