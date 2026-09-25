using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Server.Packet.Send.Chat;

public class PacketGetPrivateChatHistoryScRsp : BasePacket
{
    public PacketGetPrivateChatHistoryScRsp(uint contactSide, uint targetSide, PlayerInstance player) : base(
        CmdIds.GetPrivateChatHistoryScRsp)
    {
        var proto = new GetPrivateChatHistoryScRsp
        {
            ContactSide = contactSide,
            TargetSide = targetSide
        };

        // ChatHistory is keyed by the other side's uid.
        var infos = player.FriendManager!.GetHistoryInfo((int)targetSide);
        proto.ChatMessageList.AddRange(infos);

        // The Global Chat contact greets with its configured welcome as the oldest entry (the list is newest-first).
        var serverOption = ConfigManager.Config.ServerOption;
        var welcome = serverOption.GlobalChat.WelcomeMessage;
        if (serverOption.GetGlobalChatUid() is { } globalUid && targetSide == globalUid &&
            !string.IsNullOrWhiteSpace(welcome))
        {
            var oldest = infos.Count > 0 ? infos.Min(x => x.CreateTime) : 0;
            proto.ChatMessageList.Add(BuildText((uint)globalUid, welcome,
                oldest > 0 ? oldest - 1 : (ulong)Extensions.GetUnixSec()));
        }

        SetData(proto);
    }

    private static ChatMessageData BuildText(uint fromUid, string message, ulong sendTime)
    {
        return new ChatMessageData
        {
            CreateTime = sendTime,
            CKHPFFENOBE = new EKNABKLPEEL
            {
                KPOBMNLKLOK = HCMEILLLKBD.Jdoaipkbipe,
                RoleId = fromUid
            },
            MessageDatas =
            {
                new MessageChatData
                {
                    MessageType = MsgType.CustomText,
                    ChatData = new ChatData { MessageText = message }
                }
            }
        };
    }
}
