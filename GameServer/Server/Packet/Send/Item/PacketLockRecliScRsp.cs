using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Item;

public class PacketLockRelicScRsp : BasePacket
{
    public PacketLockRelicScRsp(bool success) : base(CmdIds.LockRelicScRsp)
    {
        LockRelicScRsp proto = new();

        if (!success) proto.Retcode = (uint)Retcode.RetFail;

        SetData(proto);
    }
}