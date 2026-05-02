using CastoriceHoney.Server.Data;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Music;

public class PacketMusicRhythmUnlockSongSfxScNotify : BasePacket
{
    public PacketMusicRhythmUnlockSongSfxScNotify() : base(CmdIds.MusicRhythmUnlockSongSfxScNotify)
    {
        var proto = new MusicRhythmUnlockSongSfxScNotify();

        SetData(proto);
    }
}

