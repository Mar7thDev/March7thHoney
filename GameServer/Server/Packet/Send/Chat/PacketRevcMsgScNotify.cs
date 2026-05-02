using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Chat;

public class PacketRevcMsgScNotify : BasePacket
{
    public PacketRevcMsgScNotify(uint toUid, uint fromUid, string msg) : base(CmdIds.RevcMsgScNotify)
    {
        var chat = BuildText(msg);

        var proto = new RevcMsgScNotify
        {
            ChatType = ChatType.Private,
            SourceUid = fromUid,
            RecvMessageData = chat
        };

        SetData(proto);
    }

    public PacketRevcMsgScNotify(uint toUid, uint fromUid, uint extraId) : base(CmdIds.RevcMsgScNotify)
    {
        var chat = BuildEmoji(extraId);

        var proto = new RevcMsgScNotify
        {
            ChatType = ChatType.Private,
            SourceUid = fromUid,
            RecvMessageData = chat
        };

        SetData(proto);
    }

    private static ChatMessageData BuildText(string msg)
    {
        return new ChatMessageData
        {
            CreateTime = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            MessageDatas =
            {
                new MessageChatData
                {
                    MessageType = MsgType.CustomText,
                    ChatData = new ChatData
                    {
                        MessageText = msg
                    }
                }
            }
        };
    }

    private static ChatMessageData BuildEmoji(uint extraId)
    {
        return new ChatMessageData
        {
            CreateTime = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            MessageDatas =
            {
                new MessageChatData
                {
                    MessageType = MsgType.Emoji,
                    ChatData = new ChatData
                    {
                        ExtraId = extraId
                    }
                }
            }
        };
    }
}

