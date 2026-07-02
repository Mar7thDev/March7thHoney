using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Lobby;

public class PacketLobbyStartFightScRsp : BasePacket
{
    public PacketLobbyStartFightScRsp(Retcode code) : base(CmdIds.LobbyStartFightScRsp)
    {
        // 4.4 LobbyStartFightScRsp = GJBGNMGGDLE { retcode = 14 }
        var proto = new GJBGNMGGDLE
        {
            Retcode = (uint)code
        };

        SetData(proto);
    }
}
