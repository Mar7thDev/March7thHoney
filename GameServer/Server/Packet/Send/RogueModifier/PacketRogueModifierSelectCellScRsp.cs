using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueModifier;

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