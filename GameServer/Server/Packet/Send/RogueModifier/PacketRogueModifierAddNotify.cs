using March7thHoney.GameServer.Game.ChessRogue.Modifier;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueModifier;

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