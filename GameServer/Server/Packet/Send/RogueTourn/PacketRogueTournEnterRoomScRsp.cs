using CastoriceHoney.Server.GameServer.Game.RogueTourn;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueTourn;

public class PacketRogueTournEnterRoomScRsp : BasePacket
{
    public PacketRogueTournEnterRoomScRsp(Retcode ret, RogueTournInstance? instance) : base(
        CmdIds.RogueTournEnterRoomScRsp)
    {
        var proto = new RogueTournEnterRoomScRsp
        {
            Retcode = (uint)ret
        };

        if (instance != null)
            proto.RogueTournCurSceneInfo = instance.ToCurSceneInfo();

        SetData(proto);
    }
}