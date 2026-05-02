using March7thHoney.GameServer.Game.Lobby;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Lobby;

public class PacketLobbyCreateScRsp : BasePacket
{
    public PacketLobbyCreateScRsp(LobbyRoomInstance room) : base(CmdIds.LobbyCreateScRsp)
    {
        var proto = new LobbyCreateScRsp
        {
            RoomId = (ulong)room.RoomId,
            FightGameMode = room.GameMode,
            LobbyBasicInfo = { room.Players.Select(x => x.ToProto()) },
            LobbyMode = (uint)room.LobbyMode
        };

        SetData(proto);
    }

    public PacketLobbyCreateScRsp(Retcode retCode) : base(CmdIds.LobbyCreateScRsp)
    {
        var proto = new LobbyCreateScRsp
        {
            Retcode = (uint)retCode
        };

        SetData(proto);
    }
}