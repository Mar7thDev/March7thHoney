using March7thHoney.Database.Avatar;
using March7thHoney.Database.Lineup;
using March7thHoney.GameServer.Game.Battle;
using March7thHoney.GameServer.Game.Lineup;
using March7thHoney.Proto;
using March7thHoney.Util;
using LineupInfo = March7thHoney.Database.Lineup.LineupInfo;

namespace March7thHoney.GameServer.Game.GridFight.Battle;

public static class GridFightBattleProtoBuilder
{
    public static List<AvatarLineupData> HandleProto(BattleInstance battle, GridFightInstance inst, SceneBattleInfo proto)
    {
        var player = battle.Player;
        var foregroundIds = inst.BuildForegroundAvatarIds();
        var backgroundIds = inst.BuildBackgroundAvatarIds();

        var foreground = ResolveAvatars(battle, foregroundIds);
        var background = ResolveAvatars(battle, backgroundIds);

        var tempLineup = new LineupInfo
        {
            LineupType = (int)ExtraLineupType.LineupChessRogue,
            BaseAvatars = foreground
                .Concat(background)
                .Select(a => new LineupAvatarInfo { BaseAvatarId = a.AvatarInfo.BaseAvatarId })
                .ToList(),
            AvatarData = player.AvatarManager?.AvatarData
        };

        foreach (var item in foreground.Concat(background))
            item.AvatarInfo.SetCurHp(10000, true);

        var collection = new PlayerDataCollection(player.Data, player.InventoryManager!.Data, tempLineup);

        foreach (var item in foreground.Take(4))
            proto.BattleAvatarList.Add(item.AvatarInfo.ToBattleProto(collection, item.AvatarType));

        return foreground;
    }

    private static List<AvatarLineupData> ResolveAvatars(BattleInstance battle, IEnumerable<int> avatarIds)
    {
        var list = new List<AvatarLineupData>();
        foreach (var id in avatarIds)
        {
            var info = ResolveAvatar(battle, id);
            if (info != null) list.Add(info);
        }
        return list;
    }

    private static AvatarLineupData? ResolveAvatar(BattleInstance battle, int avatarId)
    {
        var avatarMgr = battle.Player.AvatarManager;
        if (avatarMgr == null) return null;

        var formal = avatarMgr.GetFormalAvatar(avatarId);
        if (formal != null) return new AvatarLineupData(formal, AvatarType.AvatarGridFightType);

        var trial = avatarMgr.GetTrialAvatar(avatarId);
        if (trial != null)
        {
            trial.CheckLevel(battle.Player.Data.WorldLevel);
            return new AvatarLineupData(trial, AvatarType.AvatarTrialType);
        }

        return null;
    }
}
