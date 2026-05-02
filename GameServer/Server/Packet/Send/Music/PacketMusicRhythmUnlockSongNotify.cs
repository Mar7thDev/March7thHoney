using March7thHoney.Data;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Music;

public class PacketMusicRhythmUnlockSongNotify : BasePacket
{
    public PacketMusicRhythmUnlockSongNotify() : base(CmdIds.MusicRhythmUnlockSongNotify)
    {
        var proto = new MusicRhythmUnlockSongNotify();

        SetData(proto);
    }
}

