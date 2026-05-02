using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueModifier;

public class PacketRogueModifierSelectCellScRsp : BasePacket
{
    public PacketRogueModifierSelectCellScRsp(uint cellId) : base(CmdIds.RogueModifierSelectCellScRsp)
    {
        var proto = new RogueModifierSelectCellScRsp
        {
            CellId = cellId
        };

        SetData(proto);
    }
}