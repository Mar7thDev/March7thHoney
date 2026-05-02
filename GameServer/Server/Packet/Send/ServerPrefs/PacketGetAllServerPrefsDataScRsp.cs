using CastoriceHoney.Server.Database.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ServerPrefs;

public class PacketGetAllServerPrefsDataScRsp : BasePacket
{
    public PacketGetAllServerPrefsDataScRsp(List<ServerPrefsInfo> infos) : base(CmdIds.GetAllServerPrefsDataScRsp)
    {
        var proto = new GetAllServerPrefsDataScRsp();

        foreach (var info in infos) proto.ServerPrefsList.Add(info.ToProto());

        SetData(proto);
    }
}