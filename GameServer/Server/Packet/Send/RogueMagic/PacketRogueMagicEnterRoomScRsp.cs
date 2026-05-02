using CastoriceHoney.Server.GameServer.Game.RogueMagic;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueMagic;

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