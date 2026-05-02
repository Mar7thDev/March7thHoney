using CastoriceHoney.Server.GameServer.Game.RogueTourn;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueTourn;

public class PacketRogueTournEnterLayerScRsp : BasePacket
{
    public PacketRogueTournEnterLayerScRsp(Retcode ret, RogueTournInstance? instance) : base(
        CmdIds.RogueTournEnterLayerScRsp)
    {
        var proto = new RogueTournEnterLayerScRsp
        {
            Retcode = (uint)ret
        };

        if (instance != null)
            proto.RogueTournCurSceneInfo = instance.ToCurSceneInfo();

        SetData(proto);
    }
}