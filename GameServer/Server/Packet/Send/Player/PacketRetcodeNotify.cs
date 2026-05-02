using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Player;

public class PacketRetcodeNotify : BasePacket
{
    public PacketRetcodeNotify(Retcode retcode) : base(CmdIds.PlayerSyncScNotify)
    {
        // RetcodeNotify is unavailable in the current proto set.
        SetData(new PlayerSyncScNotify());
    }
}

