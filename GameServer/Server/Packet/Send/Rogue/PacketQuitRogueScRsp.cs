using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Rogue;

public class PacketQuitRogueScRsp : BasePacket
{
    public PacketQuitRogueScRsp(PlayerInstance player) : base(CmdIds.QuitRogueScRsp)
    {
        var proto = new QuitRogueScRsp
        {
            RogueGameInfo = player.RogueManager!.ToProto()
        };

        SetData(proto);
    }
}