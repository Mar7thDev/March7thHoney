using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Activity;

/// <summary>Empty successful reply to the 4.7 query <see cref="CKMHMAJPJEP" />: retcode 0, no entries.</summary>
public class PacketPAMDEKPLLEL : BasePacket
{
    public PacketPAMDEKPLLEL() : base(CmdIds.PAMDEKPLLEL)
    {
        SetData(new PAMDEKPLLEL());
    }
}
