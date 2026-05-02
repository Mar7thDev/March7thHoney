using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Music;

public class PacketMusicRhythmStartLevelScRsp : BasePacket
{
    public PacketMusicRhythmStartLevelScRsp(uint levelId) : base(CmdIds.MusicRhythmStartLevelScRsp)
    {
        var proto = new MusicRhythmStartLevelScRsp
        {
            LevelId = levelId
        };

        SetData(proto);
    }
}