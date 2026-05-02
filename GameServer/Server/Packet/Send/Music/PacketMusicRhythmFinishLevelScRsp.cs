using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Music;

public class PacketMusicRhythmFinishLevelScRsp : BasePacket
{
    public PacketMusicRhythmFinishLevelScRsp(uint curLevel) : base(CmdIds.MusicRhythmFinishLevelScRsp)
    {
        var proto = new MusicRhythmFinishLevelScRsp
        {
            LevelId = curLevel
        };

        SetData(proto);
    }
}