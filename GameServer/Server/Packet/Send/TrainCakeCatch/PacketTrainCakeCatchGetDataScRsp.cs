using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.TrainCakeCatch;

public class PacketTrainCakeCatchGetDataScRsp : BasePacket
{
    public PacketTrainCakeCatchGetDataScRsp() : base(CmdIds.TrainCakeCatchGetDataScRsp)
    {
        SetData(new TrainCakeCatchGetDataScRsp());
    }
}
