using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.JukeBox;

public class PacketPlayBackGroundMusicScRsp : BasePacket
{
    public PacketPlayBackGroundMusicScRsp(uint musicId) : base(CmdIds.PlayBackGroundMusicScRsp)
    {
        // 4.3: JukeBox proto 混淆名变更 (同 GetJukeboxDataScRsp)
        var proto = new PlayBackGroundMusicScRsp
        {
            GFFOBALDBPM = new MNCBEDBDDHL
            {
                JKNLCEEDAHJ = new KHADHDLCNOI
                {
                    Id = musicId
                }
            }
        };

        SetData(proto);
    }
}

