using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.PlayerBoard;

public class PacketSetAssistAvatarScRsp : BasePacket
{
    public PacketSetAssistAvatarScRsp(IReadOnlyList<uint> avatarIds, Retcode retcode = Retcode.RetSucc)
        : base(CmdIds.SetAssistAvatarScRsp)
    {
        var proto = new SetAssistAvatarScRsp { Retcode = (uint)retcode };
        proto.AvatarIdList.AddRange(avatarIds);
        if (avatarIds.Count > 0) proto.AvatarId = avatarIds[0];

        SetData(proto);
    }
}
