using CastoriceHoney.Server.GameServer.Game.ChessRogue;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ChessRogue;

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