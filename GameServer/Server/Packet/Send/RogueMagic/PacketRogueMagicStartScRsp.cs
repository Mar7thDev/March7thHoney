using CastoriceHoney.Server.GameServer.Game.RogueMagic;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;
using Google.Protobuf;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueMagic;

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