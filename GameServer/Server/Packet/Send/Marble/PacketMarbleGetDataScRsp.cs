using CastoriceHoney.Server.Data;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Marble;

public class PacketMarbleGetDataScRsp : BasePacket
{
    public PacketMarbleGetDataScRsp() : base(CmdIds.MarbleGetDataScRsp)
    {
        var proto = new MarbleGetDataScRsp
        {
            OwnedSealList = { GameData.MarbleSealData.Keys.Select(x => (uint)x) },
            MarbleFinishLevelIdList = { GameData.MarbleMatchInfoData.Keys.Select(x => (uint)x) }
        };

        SetData(proto);
    }
}