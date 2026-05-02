using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueModifier;

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