using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Scene;

public class PacketGetCurSceneInfoScRsp : BasePacket
{
    public PacketGetCurSceneInfoScRsp(PlayerInstance player) : base(CmdIds.GetCurSceneInfoScRsp)
    {
        var proto = new GetCurSceneInfoScRsp
        {
            Scene = player.SceneInstance!.ToProto()
        };

        SetData(proto);
    }
}