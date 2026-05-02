using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueModifier;

public class PacketRogueModifierStageStartNotify : BasePacket
{
    public PacketRogueModifierStageStartNotify(RogueModifierSourceType source) : base(
        CmdIds.RogueModifierStageStartNotify)
    {
        var proto = new RogueModifierStageStartNotify
        {
            ModifierSourceType = source
        };

        SetData(proto);
    }
}