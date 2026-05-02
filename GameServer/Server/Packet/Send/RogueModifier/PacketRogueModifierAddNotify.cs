using CastoriceHoney.Server.GameServer.Game.ChessRogue.Modifier;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueModifier;

public class PacketRogueModifierAddNotify : BasePacket
{
    public PacketRogueModifierAddNotify(ChessRogueDiceModifierInstance modifier) : base(CmdIds.RogueModifierAddNotify)
    {
        var proto = new RogueModifierAddNotify
        {
            Modifier = modifier.ToProto()
        };

        SetData(proto);
    }
}