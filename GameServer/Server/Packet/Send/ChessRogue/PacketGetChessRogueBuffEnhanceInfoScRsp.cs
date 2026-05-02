using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ChessRogue;

public class PacketGetChessRogueBuffEnhanceInfoScRsp : BasePacket
{
    public PacketGetChessRogueBuffEnhanceInfoScRsp(PlayerInstance player) : base(CmdIds
        .GetChessRogueBuffEnhanceInfoScRsp)
    {
        var proto = new GetChessRogueBuffEnhanceInfoScRsp();
        if (player.ChessRogueManager!.RogueInstance == null)
        {
            proto.Retcode = 1;
            SetData(proto);
            return;
        }

        proto.BuffEnhanceInfo = player.ChessRogueManager.RogueInstance!.ToChessEnhanceInfo();

        SetData(proto);
    }
}