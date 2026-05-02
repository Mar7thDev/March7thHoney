using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Lobby;

public class PacketLobbyStartFightScRsp : BasePacket
{
    public PacketLobbyStartFightScRsp(Retcode code) : base(CmdIds.LobbyStartFightScRsp)
    {
        var proto = new LobbyStartFightScRsp
        {
            Retcode = (uint)code
        };

        SetData(proto);
    }
}