using March7thHoney.Data;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Player;

public class PacketGetVideoVersionKeyScRsp : BasePacket
{
    public PacketGetVideoVersionKeyScRsp() : base(CmdIds.GetVideoVersionKeyScRsp)
    {
        var proto = new GetVideoVersionKeyScRsp();

        // The response used to carry nothing but the ids, so the client received a list of videos with
        // no key attached at all. Field mapping on the 4.5v3 proto, taken from the 4.5.0 dump whose four
        // field numbers and types match this one exactly: IBDIMHPOPOJ = 8, ODOOBNALOAA = video_key = 10,
        // AFBDJOIHOID = ps_field_3 = 14, id = 12.
        // Do NOT carry this mapping to a 4.6 proto generation: there VideoKeyInfo is
        // { HHCAKADKHAN = 6, IBDIMHPOPOJ = 8, BOGECOHIINH = 13, id = 14 }, and only IBDIMHPOPOJ / id
        // keep their names, so the remaining two cannot be joined across the dumps.
        foreach (var video in GameData.VideoKeysConfig.VideoKeyInfoData)
            proto.VideoKeyInfoList.Add(new VideoKeyInfo
            {
                Id = (uint)video.Id,
                ODOOBNALOAA = video.VideoKey,
                IBDIMHPOPOJ = video.IBDIMHPOPOJ,
                AFBDJOIHOID = video.PsField3
            });

        foreach (var activity in GameData.VideoKeysConfig.ActivityVideoKeyData)
            proto.ActivityVideoKeyInfoList.Add(new VideoKeyInfo
            {
                Id = (uint)activity.Id,
                ODOOBNALOAA = activity.VideoKey,
                IBDIMHPOPOJ = activity.IBDIMHPOPOJ,
                AFBDJOIHOID = activity.PsField3
            });

        SetData(proto);
    }
}
