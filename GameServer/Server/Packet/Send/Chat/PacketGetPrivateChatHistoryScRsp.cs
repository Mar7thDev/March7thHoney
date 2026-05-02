using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Chat;

public class PacketGetPrivateChatHistoryScRsp : BasePacket
{
    public PacketGetPrivateChatHistoryScRsp(uint contactId, PlayerInstance player) : base(
        CmdIds.GetPrivateChatHistoryScRsp)
    {
        var proto = new GetPrivateChatHistoryScRsp
        {
            ContactSide = contactId
        };

        var infos = player.FriendManager!.GetHistoryInfo((int)contactId);
        proto.ChatMessageList.AddRange(infos);

        SetData(proto);
    }
}