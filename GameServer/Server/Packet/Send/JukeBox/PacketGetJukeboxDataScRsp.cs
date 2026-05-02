using CastoriceHoney.Server.Data;
using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.JukeBox;

public class PacketGetJukeboxDataScRsp : BasePacket
{
    public PacketGetJukeboxDataScRsp(PlayerInstance player) : base(CmdIds.GetJukeboxDataScRsp)
    {
        var proto = new GetJukeboxDataScRsp
        {
            JILKOPINLDC = new MCPPIEJEBEF
            {
                LEKGBMCOJDB = new NKLAFFLJCHG
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

