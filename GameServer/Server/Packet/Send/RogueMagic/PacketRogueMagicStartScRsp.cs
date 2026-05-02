using March7thHoney.GameServer.Game.RogueMagic;
using March7thHoney.Kcp;
using March7thHoney.Proto;
using Google.Protobuf;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueMagic;

public class PacketRogueMagicStartScRsp : BasePacket
{
    public PacketRogueMagicStartScRsp(Retcode retcode, RogueMagicInstance? instance) : base(CmdIds.RogueMagicStartScRsp)
    {
        var rsp = new RogueMagicStartScRsp
        {
            Retcode = (uint)retcode
        };

        if (instance != null)
        {
            rsp.RogueTournCurSceneInfo = instance.ToCurSceneInfo();
            rsp.RogueTournCurInfo = instance.ToProto();
        }

        Data = rsp.ToByteArray();
    }
}