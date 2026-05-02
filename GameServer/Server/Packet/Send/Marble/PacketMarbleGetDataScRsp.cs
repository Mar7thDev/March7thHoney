using March7thHoney.Data;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Marble;

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