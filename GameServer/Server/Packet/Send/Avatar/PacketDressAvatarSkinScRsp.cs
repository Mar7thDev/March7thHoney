using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Avatar;

public class PacketDressAvatarSkinScRsp : BasePacket
{
    public PacketDressAvatarSkinScRsp(Retcode retcode) : base(CmdIds.DressAvatarSkinScRsp)
    {
        SetData(new DressAvatarSkinScRsp { PLDKENJLKME = (uint)retcode });
    }
}
