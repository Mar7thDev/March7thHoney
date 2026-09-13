using Google.Protobuf;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Send.Activity.MechCraft;

public sealed class PacketMechCraftData : BasePacket
{
    public PacketMechCraftData(ushort commandId, IMessage message) : base(commandId)
    {
        SetData(message);
    }
}
