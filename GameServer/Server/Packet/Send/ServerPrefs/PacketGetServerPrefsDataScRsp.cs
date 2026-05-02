using CastoriceHoney.Server.Database.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;
using Google.Protobuf;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ServerPrefs;

public class PacketGetServerPrefsDataScRsp : BasePacket
{
    public PacketGetServerPrefsDataScRsp(ServerPrefsInfo? info, uint id) : base(CmdIds.GetServerPrefsDataScRsp)
    {
        var proto = new GetServerPrefsDataScRsp
        {
            ServerPrefs = info?.ToProto() ?? new Proto.ServerPrefs
            {
                Data = ByteString.Empty,
                ServerPrefsId = id
            }
        };

        SetData(proto);
    }
}