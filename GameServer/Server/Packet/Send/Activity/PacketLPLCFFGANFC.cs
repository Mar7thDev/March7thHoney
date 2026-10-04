using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Activity;

/// <summary>Empty successful reply to the 4.7 query <see cref="MGCJKEEOPEC" />: retcode 0, no progress.</summary>
public class PacketLPLCFFGANFC : BasePacket
{
    public PacketLPLCFFGANFC() : base(CmdIds.LPLCFFGANFC)
    {
        SetData(new LPLCFFGANFC());
    }
}
