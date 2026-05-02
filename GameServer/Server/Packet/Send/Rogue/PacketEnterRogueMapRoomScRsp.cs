using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Rogue;

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