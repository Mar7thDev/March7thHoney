using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Avatar;

public class PacketTakeOffAvatarSkinScRsp : BasePacket
{
    public PacketTakeOffAvatarSkinScRsp(Retcode retcode) : base(CmdIds.TakeOffAvatarSkinScRsp)
    {
        SetData(new TakeOffAvatarSkinScRsp { PLDKENJLKME = (uint)retcode });
    }
}
