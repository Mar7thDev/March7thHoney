using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;
using Google.Protobuf.Collections;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.PlayerBoard;

public class PacketSetAssistAvatarScRsp : BasePacket
{
    public PacketSetAssistAvatarScRsp(RepeatedField<uint> avatarId) : base(CmdIds.SetAssistAvatarScRsp)
    {
        var proto = new SetAssistAvatarScRsp();
        proto.AvatarIdList.AddRange(avatarId);

        SetData(proto);
    }
}