using CastoriceHoney.Server.GameServer.Game.Lobby;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Lobby;

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