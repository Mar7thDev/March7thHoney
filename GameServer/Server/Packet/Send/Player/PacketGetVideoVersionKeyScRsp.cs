using March7thHoney.Data;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Player;

public class PacketGetVideoVersionKeyScRsp : BasePacket
{
    public PacketGetVideoVersionKeyScRsp() : base(CmdIds.GetVideoVersionKeyScRsp)
    {
        var proto = new GetVideoVersionKeyScRsp();

        // Official 4.6 story capture seq 133 matches the configured values by video id:
        // HHCAKADKHAN = videoKey, BOGECOHIINH = psField3. The 4.7 raw definitions retain
        // these property identities with new wire tags (10 and 7 respectively).
        foreach (var video in GameData.VideoKeysConfig.VideoKeyInfoData)
            proto.VideoKeyInfoList.Add(new VideoKeyInfo
            {
                Id = (uint)video.Id,
                IBDIMHPOPOJ = video.IBDIMHPOPOJ,
                HHCAKADKHAN = video.VideoKey,
                BOGECOHIINH = video.PsField3
            });

        foreach (var activity in GameData.VideoKeysConfig.ActivityVideoKeyData)
            proto.ActivityVideoKeyInfoList.Add(new VideoKeyInfo
            {
                Id = (uint)activity.Id,
                IBDIMHPOPOJ = activity.IBDIMHPOPOJ,
                HHCAKADKHAN = activity.VideoKey,
                BOGECOHIINH = activity.PsField3
            });

        SetData(proto);
    }
}
