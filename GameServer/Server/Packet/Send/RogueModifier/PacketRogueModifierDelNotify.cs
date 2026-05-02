using CastoriceHoney.Server.GameServer.Game.ChessRogue.Modifier;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueModifier;

public class PacketRogueModifierDelNotify : BasePacket
{
    public PacketRogueModifierDelNotify(ChessRogueDiceModifierInstance modifier) : base(CmdIds.RogueModifierDelNotify)
    {
        var proto = new RogueModifierDelNotify
        {
            ModifierId = (ulong)modifier.ModifierId
        };

        SetData(proto);
    }
}