using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ServerPrefs;

public class PacketUpdateServerPrefsDataScRsp : BasePacket
{
    public PacketUpdateServerPrefsDataScRsp(uint prefsId) : base(CmdIds.UpdateServerPrefsDataScRsp)
    {
        var proto = new UpdateServerPrefsDataScRsp
        {
            ServerPrefsId = prefsId
        };

        SetData(proto);
    }
}