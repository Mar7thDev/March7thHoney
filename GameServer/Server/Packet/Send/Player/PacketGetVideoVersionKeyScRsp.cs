using March7thHoney.Data;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Player;

public class PacketGetVideoVersionKeyScRsp : BasePacket
{
    public PacketGetVideoVersionKeyScRsp() : base(CmdIds.GetVideoVersionKeyScRsp)
    {
        var proto = new GetVideoVersionKeyScRsp();

        // 4.6 VideoKeyInfo: HHCAKADKHAN(6) = video_key, BOGECOHIINH(13) = ps_field_3, IBDIMHPOPOJ(8) unchanged.
        // Settled by comparing the official 4.5 and 4.6 GetVideoVersionKeyScRsp captures: id 5600 carries the
        // same three values in both, with videoKey/psField_3 landing on fields 6/13 in 4.6 (yuukisr_go
        // 6a76330, data/proto/460.nt.txt). Without the key the client cannot decrypt the cutscene videos.
        foreach (var video in GameData.VideoKeysConfig.VideoKeyInfoData)
            proto.VideoKeyInfoList.Add(BuildKey(video.Id, video.VideoKey, video.IBDIMHPOPOJ, video.PsField3));

        foreach (var activity in GameData.VideoKeysConfig.ActivityVideoKeyData)
            proto.ActivityVideoKeyInfoList.Add(BuildKey(activity.Id, activity.VideoKey, activity.IBDIMHPOPOJ,
                activity.PsField3));

        SetData(proto);
    }

    private static VideoKeyInfo BuildKey(int id, ulong videoKey, ulong ibdimhpopoj, ulong psField3)
    {
        return new VideoKeyInfo
        {
            Id = (uint)id,
            HHCAKADKHAN = videoKey,
            IBDIMHPOPOJ = ibdimhpopoj,
            BOGECOHIINH = psField3
        };
    }
}
