using CastoriceHoney.Server.GameServer.Game.RogueTourn;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueTourn;

public class PacketRogueTournStartScRsp : BasePacket
{
    public PacketRogueTournStartScRsp(Retcode retcode, RogueTournInstance? rogueTourn) : base(
        CmdIds.RogueTournStartScRsp)
    {
        var proto = new RogueTournStartScRsp
        {
            Retcode = (uint)retcode
        };

        if (rogueTourn != null)
        {
            proto.RogueTournCurInfo = rogueTourn.ToProto();
            proto.RogueTournCurSceneInfo = rogueTourn.ToCurSceneInfo();
        }

        SetData(proto);
    }
}