using CastoriceHoney.Server.GameServer.Game.Lobby;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Lobby;

public class PacketLobbySyncInfoScNotify : BasePacket
{
    public PacketLobbySyncInfoScNotify(int uid, LobbyRoomInstance room, LobbyModifyType modifyType) : base(
        CmdIds.LobbySyncInfoScNotify)
    {
        var proto = new LobbySyncInfoScNotify
        {
            LobbyBasicInfo = { room.Players.Select(x => x.ToProto()) },
            Uid = (uint)uid,
            Type = modifyType
        };

        SetData(proto);
    }
}