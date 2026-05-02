using March7thHoney.GameServer.Game.RogueTourn;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueTourn;

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