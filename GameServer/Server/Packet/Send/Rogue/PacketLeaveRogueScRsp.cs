using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Rogue;

public class PacketLeaveRogueScRsp : BasePacket
{
    public PacketLeaveRogueScRsp(PlayerInstance player) : base(CmdIds.LeaveRogueScRsp)
    {
        var proto = new LeaveRogueScRsp
        {
            Lineup = player.LineupManager!.GetCurLineup()!.ToProto(),
            Scene = player.SceneInstance!.ToProto(),
            RogueGameInfo = player.RogueManager!.ToProto()
        };

        SetData(proto);
    }
}