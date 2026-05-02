using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightUseConsumableScRsp : BasePacket
{
    public PacketGridFightUseConsumableScRsp(Retcode ret) : base(CmdIds.GridFightUseConsumableScRsp)
    {
        var proto = new GridFightUseConsumableScRsp
        {
            Retcode = (uint)ret
        };

        SetData(proto);
    }
}