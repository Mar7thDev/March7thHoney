using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Lobby;

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