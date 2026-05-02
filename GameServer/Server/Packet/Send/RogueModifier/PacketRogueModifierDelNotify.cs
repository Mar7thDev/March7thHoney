using March7thHoney.GameServer.Game.ChessRogue.Modifier;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueModifier;

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