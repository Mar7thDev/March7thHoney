using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Friend;

public class PacketSetAssistScRsp : BasePacket
{
    public PacketSetAssistScRsp(Retcode retcode, uint uid, uint avatarId) : base(CmdIds.SetAssistScRsp)
    {
        var proto = new SetAssistScRsp { Retcode = (uint)retcode };
        if (retcode == Retcode.RetSucc)
        {
            proto.Uid = uid;
            proto.AvatarId = avatarId;
        }

        SetData(proto);
    }
}
