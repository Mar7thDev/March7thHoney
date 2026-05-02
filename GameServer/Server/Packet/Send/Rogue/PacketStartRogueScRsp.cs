using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Rogue;

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