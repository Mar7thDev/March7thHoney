using Google.Protobuf;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Send.MiscModule;

/// <summary>
///     Data response for a module the server does not model yet: retcode 0 with every singular sub-message present
///     and empty, instead of the empty body an unhandled request gets.
/// </summary>
public class PacketPresentEmptyScRsp : BasePacket
{
    public PacketPresentEmptyScRsp(int cmdId, IMessage rsp) : base((ushort)cmdId)
    {
        SetData(ProtoPresence.FillEmptySubMessages(rsp));
    }
}
