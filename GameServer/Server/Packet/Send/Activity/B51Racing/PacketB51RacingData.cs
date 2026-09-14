using Google.Protobuf;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Send.Activity.B51Racing;

public sealed class PacketB51RacingData : BasePacket
{
    public PacketB51RacingData(ushort commandId, IMessage message) : base(commandId) => SetData(message);
}
