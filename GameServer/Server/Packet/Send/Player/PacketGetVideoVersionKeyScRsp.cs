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
        // The two remaining uint64 fields changed their obfuscated names and field numbers from 4.5,
        // so they must stay unset until a 4.6 capture or an offset join proves their mapping. Guessing
        // here can wedge the client UI. IBDIMHPOPOJ and id are the two fields confirmed across versions.
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
