using March7thHoney.GameServer.Game.RogueMagic;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueMagic;

public class PacketRogueMagicEnterLayerScRsp : BasePacket
{
    public PacketRogueMagicEnterLayerScRsp(Retcode ret, RogueMagicInstance? instance) : base(
        CmdIds.RogueMagicEnterLayerScRsp)
    {
        var proto = new RogueMagicEnterLayerScRsp
        {
            Retcode = (uint)ret
        };

        if (instance != null)
            proto.RogueTournCurSceneInfo = instance.ToCurSceneInfo();

        SetData(proto);
    }
}