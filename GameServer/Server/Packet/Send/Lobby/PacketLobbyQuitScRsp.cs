using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Lobby;

public class PacketLobbyQuitScRsp : BasePacket
{
    public PacketLobbyQuitScRsp(Retcode code) : base(CmdIds.LobbyQuitScRsp)
    {
        var proto = new LobbyModifyPlayerInfoScRsp
        {
            Retcode = (uint)code
        };

        SetData(proto);
    }
}