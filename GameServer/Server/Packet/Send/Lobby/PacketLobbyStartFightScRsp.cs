using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Lobby;

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