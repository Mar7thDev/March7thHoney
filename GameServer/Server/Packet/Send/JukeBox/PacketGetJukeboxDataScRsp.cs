using March7thHoney.Data;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.JukeBox;

public class PacketGetJukeboxDataScRsp : BasePacket
{
    public PacketGetJukeboxDataScRsp(PlayerInstance player) : base(CmdIds.GetJukeboxDataScRsp)
    {
        // 4.3: JukeBox proto 混淆名变更（仍混淆的部分保留新名）
        //   JILKOPINLDC -> GFFOBALDBPM (now-playing wrapper, type MCPPIEJEBEF -> MNCBEDBDDHL)
        //   LEKGBMCOJDB -> JKNLCEEDAHJ (type NKLAFFLJCHG -> KHADHDLCNOI)
        //   UnlockedMusicList / MusicData / IsPlayed 已恢复可读名 (proto 端已改回)
        var proto = new GetJukeboxDataScRsp
        {
            GFFOBALDBPM = new MNCBEDBDDHL
            {
                JKNLCEEDAHJ = new KHADHDLCNOI
                {
                    Id = (uint)player.Data.CurrentBgm
                }
            }
        };

        foreach (var music in GameData.BackGroundMusicData.Values)
            proto.UnlockedMusicList.Add(new MusicData
            {
                Id = (uint)music.ID,
                GroupId = (uint)music.GroupID,
                IsPlayed = true
            });

        SetData(proto);
    }
}

