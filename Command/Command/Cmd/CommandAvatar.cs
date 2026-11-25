using March7thHoney.Data;
using March7thHoney.Data.Avatar;
using March7thHoney.Database;
using March7thHoney.Database.Avatar;
using March7thHoney.Enums.Avatar;
using March7thHoney.GameServer.Server.Packet.Send.Player;
using March7thHoney.GameServer.Server.Packet.Send.PlayerSync;
using March7thHoney.Internationalization;
using March7thHoney.Proto;

namespace March7thHoney.Command.Command.Cmd;

[CommandInfo("avatar", "Game.Command.Avatar.Desc", "Game.Command.Avatar.Usage", ["av", "ava"], permission: CommandPermissions.Avatar)]
public class CommandAvatar : ICommand
{
    [CommandMethod("talent")]
    public async ValueTask SetTalent(CommandArg arg)
    {
        if (arg.Target == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        if (arg.BasicArgs.Count < 2)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        // change basic type
        var avatarId = arg.GetInt(0);
        var level = arg.GetInt(1);
        if (level is < 0 or > 10)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.InvalidLevel",
                I18NManager.Translate("Word.Talent")));
            return;
        }

        var player = arg.Target.Player!;
        if (avatarId == -1)
        {
            foreach (var path in player.AvatarManager!.Data.FormalAvatars.SelectMany(avatarInfo =>
                         avatarInfo.PathInfos))
            {
                if (!GameData.AvatarConfigData.TryGetValue(path.Key, out var pathExcel)) continue;
                foreach (var talent in pathExcel.SkillTree.GetValueOrDefault(path.Value.EnhanceId, []))
                    path.Value.GetSkillTree()[talent.PointID] = Math.Min(level, talent.MaxLevel);
            }

            await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.AllAvatarsLevelSet",
                I18NManager.Translate("Word.Talent"), level.ToString()));

            // sync
            await player.SendPacket(new PacketPlayerSyncScNotify(player.AvatarManager.Data.FormalAvatars));

            return;
        }

        if (GameData.MultiplePathAvatarConfigData.TryGetValue(avatarId, out var multiple))
            avatarId = multiple.BaseAvatarID;

        var avatar = player.AvatarManager!.GetFormalAvatar(avatarId);
        if (avatar == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.AvatarNotFound"));
            return;
        }

        var avatarPathInfo = avatar.PathInfos.FirstOrDefault(x => x.Key == (multiple?.AvatarID ?? avatarId));
        if (avatarPathInfo.Value == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.AvatarNotFound"));
            return;
        }

        GameData.AvatarConfigData.TryGetValue(avatarPathInfo.Key, out var excel);
        if (excel == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.AvatarNotFound"));
            return;
        }

        foreach (var talent in excel.SkillTree.GetValueOrDefault(avatarPathInfo.Value.EnhanceId, []))
            avatarPathInfo.Value.GetSkillTree()[talent.PointID] = Math.Min(level, talent.MaxLevel);

        // sync
        await player.SendPacket(new PacketPlayerSyncScNotify(avatar));

        await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.AvatarLevelSet",
            excel.Name?.Replace("{NICKNAME}", player.Data.Name) ?? avatarId.ToString(),
            I18NManager.Translate("Word.Talent"), level.ToString()));
    }

    [CommandMethod("get")]
    public async ValueTask GetAvatar(CommandArg arg)
    {
        if (arg.Target == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        if (arg.BasicArgs.Count < 1)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        var id = arg.GetInt(0);
        var excel = await arg.Target.Player!.AvatarManager!.AddAvatar(id);

        if (excel == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.AvatarFailedGet", id.ToString()));
            return;
        }

        await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.AvatarGet", excel.Name ?? id.ToString()));
    }

    [CommandMethod("rank")]
    public async ValueTask SetRank(CommandArg arg)
    {
        if (arg.Target == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        if (arg.BasicArgs.Count < 2)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        var id = arg.GetInt(0);
        var rank = arg.GetInt(1);
        if (rank is < 0 or > 6)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.InvalidLevel",
                I18NManager.Translate("Word.Rank")));
            return;
        }

        if (id == -1)
        {
            arg.Target.Player!.AvatarManager!.Data.FormalAvatars.ForEach(avatar =>
            {
                foreach (var path in avatar.PathInfos.Values) path.Rank = Math.Min(rank, 6);
            });
            await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.AllAvatarsLevelSet",
                I18NManager.Translate("Word.Rank"), rank.ToString()));

            // sync
            await arg.Target.SendPacket(
                new PacketPlayerSyncScNotify(arg.Target.Player!.AvatarManager.Data.FormalAvatars));
        }
        else
        {
            if (GameData.MultiplePathAvatarConfigData.TryGetValue(id, out var multiple)) id = multiple.BaseAvatarID;

            var avatar = arg.Target.Player!.AvatarManager!.GetFormalAvatar(id);
            if (avatar == null)
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.AvatarNotFound"));
                return;
            }

            var avatarPathInfo = avatar.PathInfos.FirstOrDefault(x => x.Key == (multiple?.AvatarID ?? id));
            if (avatarPathInfo.Value == null)
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.AvatarNotFound"));
                return;
            }

            avatarPathInfo.Value.Rank = Math.Min(rank, 6);

            // sync
            await arg.Target.SendPacket(new PacketPlayerSyncScNotify(avatar));

            GameData.AvatarConfigData.TryGetValue(id, out var avatarExcel);

            await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.AvatarLevelSet",
                avatarExcel?.Name?.Replace("{NICKNAME}", arg.Target.Player!.Data.Name) ?? id.ToString(),
                I18NManager.Translate("Word.Rank"), rank.ToString()));
        }
    }

    [CommandMethod("level")]
    public async ValueTask SetLevel(CommandArg arg)
    {
        if (arg.Target == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        if (arg.BasicArgs.Count < 2)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        var id = arg.GetInt(0);
        var level = arg.GetInt(1);
        if (level is < 1 or > 80)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.InvalidLevel",
                I18NManager.Translate("Word.Avatar")));
            return;
        }

        if (id == -1)
        {
            arg.Target.Player!.AvatarManager!.Data.FormalAvatars.ForEach(avatar =>
            {
                avatar.Level = Math.Min(level, 80);
                avatar.Promotion = GameData.GetMinPromotionForLevel(avatar.Level);
            });
            await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.AllAvatarsLevelSet",
                I18NManager.Translate("Word.Avatar"), level.ToString()));

            // sync
            await arg.Target.SendPacket(
                new PacketPlayerSyncScNotify(arg.Target.Player!.AvatarManager.Data.FormalAvatars));
        }
        else
        {
            var avatar = arg.Target.Player!.AvatarManager!.GetFormalAvatar(id);
            if (avatar == null)
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.AvatarNotFound"));
                return;
            }

            avatar.Level = Math.Min(level, 80);
            avatar.Promotion = GameData.GetMinPromotionForLevel(avatar.Level);

            // sync
            await arg.Target.SendPacket(new PacketPlayerSyncScNotify(avatar));

            GameData.AvatarConfigData.TryGetValue(id, out var avatarExcel);
            await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.AvatarLevelSet",
                avatarExcel?.Name?.Replace("{NICKNAME}", arg.Target.Player!.Data.Name) ?? id.ToString(),
                I18NManager.Translate("Word.Avatar"), level.ToString()));
        }
    }

    [CommandMethod("path")]
    public async ValueTask SetPath(CommandArg arg)
    {
        if (arg.Target == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        if (arg.BasicArgs.Count < 2)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        var avatarId = arg.GetInt(0);
        var pathId = arg.GetInt(1);

        var avatar = arg.Target.Player!.AvatarManager!.GetFormalAvatar(avatarId);
        if (avatar == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.AvatarNotFound"));
            return;
        }

        if (!GameData.MultiplePathAvatarConfigData.ContainsKey(pathId))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.AvatarNotFound"));
            return;
        }

        var storedId = await arg.Target.Player.AvatarManager!.ChangePathType(avatarId, (MultiPathAvatarTypeEnum)pathId);
        if (storedId == 0)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.AvatarNotFound"));
            return;
        }

        await arg.Target.SendPacket(new PacketAvatarPathChangedNotify((uint)avatar.BaseAvatarId,
            (MultiPathAvatarType)storedId));
        await arg.Target.SendPacket(new PacketPlayerSyncScNotify(avatar));
    }

    /// <summary>
    ///     Activates or clears ExRank (Affinitive Eidolon) without the material, the operator switch for testing it.
    ///     Activation in game consumes the material; this writes the same per-avatar state and syncs the avatars.
    /// </summary>
    [CommandMethod("exrank")]
    public async ValueTask SetExRank(CommandArg arg)
    {
        if (arg.Target == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        var mode = arg.Args.Count > 1 ? arg.Args[1].ToLowerInvariant() : "";
        if (mode is not ("unlock" or "lock"))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        if (AvatarExRankMaterials.Definitions.Count == 0)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.ExRankUnavailable"));
            return;
        }

        var manager = arg.Target.Player!.AvatarManager!;
        var avatarId = arg.BasicArgs.Count > 0 ? arg.GetInt(0) : -1;
        var selected = avatarId == -1 ? null : manager.GetFormalAvatar(avatarId);
        if (avatarId != -1 && selected == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.AvatarNotFound"));
            return;
        }

        var unlock = mode == "unlock";
        var changed = 0;
        var changedAvatars = new HashSet<int>();
        foreach (var definition in AvatarExRankMaterials.Definitions.Values.OrderBy(definition => definition.Id))
        {
            // An ExRank belongs to one path of one avatar; never activate another character's.
            var owned = manager.GetFormalAvatar(definition.AvatarId);
            if (owned == null || owned.GetPathInfo(definition.AvatarId) == null) continue;
            if (selected != null && selected.BaseAvatarId != owned.BaseAvatarId) continue;

            var current = manager.Data.ExRankIds.GetValueOrDefault(owned.BaseAvatarId) ?? [];
            if (current.Contains(definition.Id) == unlock) continue;
            // A new list, as ActivateExRank does: avatars bound earlier keep the list they were given.
            List<int> next = unlock ? [.. current, definition.Id] : current.Where(id => id != definition.Id).ToList();
            if (next.Count == 0) manager.Data.ExRankIds.Remove(owned.BaseAvatarId);
            else manager.Data.ExRankIds[owned.BaseAvatarId] = next;
            changed++;
            changedAvatars.Add(owned.BaseAvatarId);
        }

        if (changed == 0)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.ExRankNoChange"));
            return;
        }

        DatabaseHelper.MarkDirty(arg.Target.Player.Uid);
        // GetFormalAvatar binds the new ExRank list before the path data is serialized.
        var synced = changedAvatars.Select(manager.GetFormalAvatar).OfType<FormalAvatarInfo>().ToList();
        await arg.Target.SendPacket(new PacketPlayerSyncScNotify(synced));
        await arg.SendMsg(I18NManager.Translate(unlock
            ? "Game.Command.Avatar.ExRankUnlocked"
            : "Game.Command.Avatar.ExRankLocked", changed.ToString()));
    }
}
