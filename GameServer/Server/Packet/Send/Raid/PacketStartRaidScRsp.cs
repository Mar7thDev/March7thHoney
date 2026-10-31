using March7thHoney.Database.Scene;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Raid;

public class PacketStartRaidScRsp : BasePacket
{
    public PacketStartRaidScRsp(RaidRecord record, PlayerInstance player) : base(CmdIds.StartRaidScRsp)
    {
        // The client builds its raid instance from `scene`; an empty response crashed it and killed the
        // leave button.
        var proto = new StartRaidScRsp();
        var lineup = player.LineupManager?.GetCurLineup();
        if (player.SceneInstance != null && lineup != null)
        {
            var lineupProto = lineup.ToProto();
            var maxMp = (uint)player.LineupManager!.GetMaxMp();
            lineupProto.MaxMp = maxMp;
            if (lineupProto.Mp > maxMp) lineupProto.Mp = maxMp;
            proto.Scene = new RaidSceneData
            {
                SceneInfo = player.SceneInstance.ToProto(),
                Lineup = lineupProto,
                RaidId = (uint)record.RaidId,
                WorldLevel = (uint)record.WorldLevel
            };
        }

        SetData(proto);
    }

    public PacketStartRaidScRsp(Retcode ret) : base(CmdIds.StartRaidScRsp)
    {
        var proto = new StartRaidScRsp
        {
            Retcode = (uint)ret
        };

        SetData(proto);
    }
}

