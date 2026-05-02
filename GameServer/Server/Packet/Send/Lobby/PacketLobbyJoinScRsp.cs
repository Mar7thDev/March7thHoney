using March7thHoney.GameServer.Game.Lobby;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Lobby;

public class PacketLobbyJoinScRsp : BasePacket
{
    public PacketLobbyJoinScRsp(Retcode retcode) : base(CmdIds.LobbyJoinScRsp)
    {
        var proto = new LobbyJoinScRsp
        {
            Retcode = (uint)retcode
        };

        SetData(proto);
    }

    public PacketLobbyJoinScRsp(LobbyRoomInstance room) : base(CmdIds.LobbyJoinScRsp)
    {
        var proto = new LobbyJoinScRsp
        {
            RoomId = (ulong)room.RoomId,
            FightGameMode = room.GameMode,
            LobbyBasicInfo = { room.Players.Select(x => x.ToProto()) },
            LobbyMode = (uint)room.LobbyMode
        };

        SetData(proto);
    }
}