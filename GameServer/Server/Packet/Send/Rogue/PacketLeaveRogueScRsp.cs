using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Rogue;

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