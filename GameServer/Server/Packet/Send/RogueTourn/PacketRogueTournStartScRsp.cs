using March7thHoney.GameServer.Game.RogueTourn;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueTourn;

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