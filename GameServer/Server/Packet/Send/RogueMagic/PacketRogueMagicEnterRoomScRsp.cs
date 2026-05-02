using March7thHoney.GameServer.Game.RogueMagic;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueMagic;

public class PacketRogueMagicEnterRoomScRsp : BasePacket
{
    public PacketRogueMagicEnterRoomScRsp(Retcode ret, RogueMagicInstance? instance) : base(
        CmdIds.RogueMagicEnterRoomScRsp)
    {
        var proto = new RogueMagicEnterRoomScRsp
        {
            Retcode = (uint)ret
        };

        if (instance != null)
            proto.RogueTournCurSceneInfo = instance.ToCurSceneInfo();

        SetData(proto);
    }
}