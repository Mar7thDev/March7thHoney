using System.Text;
using March7thHoney.Database;
using March7thHoney.Database.Lineup;
using March7thHoney.GameServer.Server;
using March7thHoney.Internationalization;
using March7thHoney.Util;

namespace March7thHoney.Command.Command.Cmd;

/// <summary>
///     Clears the trial avatars left in the player's normal lineups. A trial is a lineup slot marked with a special
///     avatar ID, not an owned avatar, and nothing in the protocol removes it: only a mission's DelTrialAvatar does.
///     A save whose story already passed that beat, or one written by an older build, keeps the Trial badge forever.
///     A trial whose character the player owns becomes that owned avatar; a story-only trial leaves the lineup.
///     Extra lineups belong to a running raid, story line or challenge and are not touched.
/// </summary>
[CommandInfo("removetrialavatar", "Game.Command.RemoveTrialAvatar.Desc", "Game.Command.RemoveTrialAvatar.Usage",
    ["rta", "cleartrial"], permission: CommandPermissions.Lineup)]
public class CommandRemoveTrialAvatar : ICommand
{
    [CommandDefault]
    public async ValueTask RemoveTrialAvatars(CommandArg arg)
    {
        if (arg.Target == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        var player = arg.Target.Player!;
        var lineupManager = player.LineupManager!;
        var avatarManager = player.AvatarManager!;
        var dryRun = arg.Args.Any(token => token.Equals("--dry-run", StringComparison.OrdinalIgnoreCase) ||
                                           token.Equals("dry-run", StringComparison.OrdinalIgnoreCase));

        var report = new StringBuilder();
        var found = 0;
        foreach (var (index, lineup) in lineupManager.Data.Lineups
                     .Where(pair => pair.Key < GameConstants.MAX_LINEUP_COUNT).OrderBy(pair => pair.Key))
        {
            var members = lineup.BaseAvatars;
            if (members == null || !members.Any(slot => slot.SpecialAvatarId != 0)) continue;

            // A promoted trial must not duplicate an owned avatar the lineup already holds.
            var present = members.Where(slot => slot.SpecialAvatarId == 0)
                .Select(slot => avatarManager.GetFormalAvatar(slot.BaseAvatarId)?.BaseAvatarId ?? slot.BaseAvatarId)
                .ToHashSet();
            var kept = new List<LineupAvatarInfo>();
            for (var slotIndex = 0; slotIndex < members.Count; slotIndex++)
            {
                var slot = members[slotIndex];
                if (slot.SpecialAvatarId == 0)
                {
                    kept.Add(slot);
                    continue;
                }

                found++;
                // The lookup resolves a path ID (8002) to the stored avatar (8001), so the Trailblazer counts as owned.
                var owned = avatarManager.GetFormalAvatar(slot.BaseAvatarId);
                var promote = owned != null && present.Add(owned.BaseAvatarId);
                report.AppendLine(I18NManager.Translate("Game.Command.RemoveTrialAvatar.Entry", index.ToString(),
                    slotIndex.ToString(), slot.SpecialAvatarId.ToString(), slot.BaseAvatarId.ToString(),
                    I18NManager.Translate(promote
                        ? "Game.Command.RemoveTrialAvatar.Promoted"
                        : "Game.Command.RemoveTrialAvatar.Removed")));
                if (dryRun || !promote || owned == null) continue;

                // A normal slot is rendered from the owned avatar with the same base ID.
                if (lineup.LeaderAvatarId == slot.BaseAvatarId) lineup.LeaderAvatarId = owned.BaseAvatarId;
                kept.Add(new LineupAvatarInfo { BaseAvatarId = owned.BaseAvatarId });
            }

            if (dryRun) continue;
            lineup.BaseAvatars = kept;
            if (kept.All(slot => slot.BaseAvatarId != lineup.LeaderAvatarId))
                lineup.LeaderAvatarId = kept.FirstOrDefault()?.BaseAvatarId ?? 0;
            // A virtual lineup only exists to host story trials.
            lineup.IsVirtual = false;
            lineup.PlaneId = 0;
        }

        if (found == 0)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.RemoveTrialAvatar.None"));
            return;
        }

        report.Insert(0, I18NManager.Translate(dryRun
            ? "Game.Command.RemoveTrialAvatar.DryRunHeader"
            : "Game.Command.RemoveTrialAvatar.Header", found.ToString()) + Environment.NewLine);
        await arg.SendMsg(report.ToString().TrimEnd());
        if (dryRun) return;

        // The client cannot render a party without a leader: move to a non-empty lineup, or give the current one the
        // Trailblazer when every lineup is empty now.
        lineupManager.EnsurePlayableCurLineup();
        if (lineupManager.GetCurLineup() is { BaseAvatars.Count: 0 } current && avatarManager.GetHero() is { } hero)
        {
            current.BaseAvatars.Add(new LineupAvatarInfo { BaseAvatarId = hero.BaseAvatarId });
            current.LeaderAvatarId = hero.BaseAvatarId;
        }

        DatabaseHelper.MarkDirty(player.Uid);
        // The client has no message that removes a trial from a lineup; a new login loads the cleaned lineups.
        ConnectionDisconnectHelper.KickByGm(arg.Target);
        await arg.SendMsg(I18NManager.Translate("Game.Command.RemoveTrialAvatar.Kicked"));
    }
}
