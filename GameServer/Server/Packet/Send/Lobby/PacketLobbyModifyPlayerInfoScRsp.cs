using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Lobby;

public class PacketLobbyModifyPlayerInfoScRsp : BasePacket
{
    public PacketLobbyModifyPlayerInfoScRsp(Retcode code) : base(CmdIds.LobbyModifyPlayerInfoScRsp)
    {
        var proto = new LobbyModifyPlayerInfoScRsp
        {
            Retcode = (uint)code
        };

        SetData(proto);
    }
}