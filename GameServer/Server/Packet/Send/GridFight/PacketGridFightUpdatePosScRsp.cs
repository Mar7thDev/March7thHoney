using March7thHoney.Kcp;
using March7thHoney.Proto;
using Google.Protobuf.Collections;

namespace March7thHoney.GameServer.Server.Packet.Send.GridFight;

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