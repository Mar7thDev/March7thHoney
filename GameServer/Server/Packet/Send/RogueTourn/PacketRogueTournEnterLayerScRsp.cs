using March7thHoney.GameServer.Game.RogueTourn;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueTourn;

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