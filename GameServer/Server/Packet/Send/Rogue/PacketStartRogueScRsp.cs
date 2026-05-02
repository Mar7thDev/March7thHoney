using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Rogue;

public class PacketStartRogueScRsp : BasePacket
{
    public PacketStartRogueScRsp(PlayerInstance player) : base(CmdIds.StartRogueScRsp)
    {
        var proto = new StartRogueScRsp
        {
            RogueGameInfo = player.RogueManager!.ToProto(),
            Lineup = player.LineupManager!.GetCurLineup()!.ToProto(),
            Scene = player.SceneInstance!.ToProto()
        };

        SetData(proto);
    }
}