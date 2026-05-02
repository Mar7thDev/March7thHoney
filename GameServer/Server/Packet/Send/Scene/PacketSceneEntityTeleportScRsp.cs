using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Scene;

public class PacketSceneEntityTeleportScRsp : BasePacket
{
    public PacketSceneEntityTeleportScRsp(EntityMotion motion) : base(CmdIds.SceneEntityTeleportScRsp)
    {
        var proto = new SceneEntityTeleportScRsp
        {
            EntityMotion = motion
        };

        SetData(proto);
    }
}