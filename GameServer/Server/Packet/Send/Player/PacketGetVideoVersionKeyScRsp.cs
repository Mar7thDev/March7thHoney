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
        // no key attached at all.
        //
        // TODO(4.6 proto): video_key and ps_field_3 are still unresolved on this proto generation.
        //   4.5  dump: IBDIMHPOPOJ=12, id=4,  video_key=11, AFBDJOIHOID=1
        //   4.5v3 dump: IBDIMHPOPOJ=8,  id=12, ODOOBNALOAA=10 (= video_key), AFBDJOIHOID=14 (= ps_field_3)
        //   4.6  dump: IBDIMHPOPOJ=8,  id=14, HHCAKADKHAN=6, BOGECOHIINH=13
        // Only IBDIMHPOPOJ and id keep their names into 4.6, so the two remaining uint64s cannot be
        // joined across the dumps the way Proto/4.6/overrides.txt resolves other messages. Guessing
        // which of HHCAKADKHAN / BOGECOHIINH is the key would be worse than sending neither - a value
        // written into the wrong field is what wedged the warp screen - so they stay unset until a
        // capture or an offset join settles it, then they belong in Proto/4.6/overrides-manual.txt.
        foreach (var video in GameData.VideoKeysConfig.VideoKeyInfoData)
            proto.VideoKeyInfoList.Add(new VideoKeyInfo
            {
                Id = (uint)video.Id,
                IBDIMHPOPOJ = video.IBDIMHPOPOJ
            });

        foreach (var activity in GameData.VideoKeysConfig.ActivityVideoKeyData)
            proto.ActivityVideoKeyInfoList.Add(new VideoKeyInfo
            {
                Id = (uint)activity.Id,
                IBDIMHPOPOJ = activity.IBDIMHPOPOJ
            });

        SetData(proto);
    }
}
