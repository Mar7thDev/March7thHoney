using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;
using Google.Protobuf.Collections;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightUpdatePosScRsp : BasePacket
{
    public PacketGridFightUpdatePosScRsp(Retcode code, RepeatedField<GridFightPosInfo> list) : base(CmdIds.GridFightUpdatePosScRsp)
    {
        var proto = new GridFightUpdatePosScRsp
        {
            Retcode = (uint)code,
            GridFightPosInfoList = { list }
        };

        SetData(proto);
    }
}