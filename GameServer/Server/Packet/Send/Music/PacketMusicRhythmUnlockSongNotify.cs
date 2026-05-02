using CastoriceHoney.Server.Data;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Music;

public class PacketMusicRhythmUnlockSongNotify : BasePacket
{
    public PacketMusicRhythmUnlockSongNotify() : base(CmdIds.MusicRhythmUnlockSongNotify)
    {
        var proto = new MusicRhythmUnlockSongNotify();

        SetData(proto);
    }
}

