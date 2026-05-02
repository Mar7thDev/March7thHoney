using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightHandlePendingActionScRsp : BasePacket
{
    public PacketGridFightHandlePendingActionScRsp(uint pos) : base(CmdIds.GridFightHandlePendingActionScRsp)
    {
        var proto = new GridFightHandlePendingActionScRsp
        {
            QueuePosition = pos
        };

        SetData(proto);
    }
}