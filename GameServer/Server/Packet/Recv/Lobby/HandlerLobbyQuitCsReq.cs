using CastoriceHoney.Server.GameServer.Server.Packet.Send.Lobby;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.Lobby;

[Opcode(CmdIds.LobbyQuitCsReq)]
public class HandlerLobbyQuitCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var room = ServerUtils.LobbyServerManager.GetPlayerJoinedRoom(connection.Player!.Uid);
        if (room == null)
        {
            await connection.SendPacket(new PacketLobbyQuitScRsp(Retcode.RetLobbyRoomNotExist));
            return;
        }

        await room.RemovePlayer(connection.Player.Uid);
        await connection.SendPacket(new PacketLobbyQuitScRsp(Retcode.RetSucc));
    }
}