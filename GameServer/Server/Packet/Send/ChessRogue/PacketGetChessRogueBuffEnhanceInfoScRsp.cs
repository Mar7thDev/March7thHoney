using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.ChessRogue;

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