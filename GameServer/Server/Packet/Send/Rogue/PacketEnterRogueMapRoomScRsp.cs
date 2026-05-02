using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Rogue;

public class PacketEnterRogueMapRoomScRsp : BasePacket
{
    public PacketEnterRogueMapRoomScRsp(PlayerInstance player) : base(CmdIds.EnterRogueMapRoomScRsp)
    {
        var proto = new EnterRogueMapRoomScRsp
        {
            CurSiteId = (uint)(player.RogueManager?.RogueInstance?.CurRoom?.SiteId ?? 0),
            Lineup = player.LineupManager!.GetCurLineup()!.ToProto(),
            Scene = player.SceneInstance!.ToProto()
        };

        SetData(proto);
    }
}