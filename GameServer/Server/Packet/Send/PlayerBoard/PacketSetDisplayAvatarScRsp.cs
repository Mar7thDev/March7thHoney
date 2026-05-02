using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;
using Google.Protobuf.Collections;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.PlayerBoard;

public class PacketSetDisplayAvatarScRsp : BasePacket
{
    public PacketSetDisplayAvatarScRsp(RepeatedField<DisplayAvatarData> avatars) : base(CmdIds.SetDisplayAvatarScRsp)
    {
        var proto = new SetDisplayAvatarScRsp();
        proto.DisplayAvatarList.AddRange(avatars);

        SetData(proto);
    }
}