using March7thHoney.GameServer.Game.ChessRogue;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.ChessRogue;

public class PacketEnhanceChessRogueBuffScRsp : BasePacket
{
    public PacketEnhanceChessRogueBuffScRsp(ChessRogueInstance rogue, uint buffId) : base(
        CmdIds.EnhanceChessRogueBuffScRsp)
    {
        var proto = new EnhanceChessRogueBuffScRsp
        {
            IsSucc = true,
            RogueBuff = new RogueCommonBuff
            {
                BuffId = buffId,
                BuffLevel = 2
            },
            BuffEnhanceInfo = rogue.ToChessEnhanceInfo()
        };

        SetData(proto);
    }
}