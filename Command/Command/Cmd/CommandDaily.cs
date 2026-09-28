using March7thHoney.GameServer.Game.DailyActive;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server.Packet.Send.DailyActive;
using March7thHoney.Internationalization;

namespace March7thHoney.Command.Command.Cmd;

[CommandInfo("daily", "Game.Command.Daily.Desc", "Game.Command.Daily.Usage", permission: CommandPermissions.Mission)]
public class CommandDaily : ICommand
{
    [CommandDefault]
    public async ValueTask Status(CommandArg arg)
    {
        var player = await GetPlayer(arg);
        if (player == null) return;

        var daily = player.GetComponent<DailyActiveManager>();
        await daily.EnsureToday();
        var quests = daily.QuestIds.Select(id =>
            $"{id}:{player.QuestManager?.GetQuestStatus(id).ToString().Replace("Quest", "") ?? "-"}");
        var taken = daily.CurrentTiers().Where(tier => daily.IsTaken(tier.Level)).Select(tier => tier.Level);

        await arg.SendMsg(I18NManager.Translate("Game.Command.Daily.Status",
            daily.Data.DayKey.ToString(), DailyResetClock.ResetHour.ToString(), daily.Data.Nonce.ToString(),
            daily.Point.ToString(), string.Join(",", taken), string.Join(", ", quests),
            daily.TodayMissionCount.ToString(), daily.TotalMissionCount.ToString(),
            string.Join(",", daily.Data.MissionsOffered)));
    }

    [CommandMethod("0 reset")]
    public async ValueTask Reset(CommandArg arg)
    {
        var player = await GetPlayer(arg);
        if (player == null) return;

        var daily = player.GetComponent<DailyActiveManager>();
        var ids = await daily.ResetNow();
        await player.SendPacket(new PacketDailyActiveInfoNotify(daily));
        await arg.SendMsg(I18NManager.Translate("Game.Command.Daily.Reset", daily.Data.Nonce.ToString(),
            string.Join(",", ids)));
    }

    [CommandMethod("0 mission")]
    public async ValueTask RollMissions(CommandArg arg)
    {
        var player = await GetPlayer(arg);
        if (player == null) return;

        if (player.MissionManager is not { Enabled: true })
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Daily.MissionDisabled"));
            return;
        }

        var force = arg.BasicArgs.Any(a => a.Equals("force", StringComparison.OrdinalIgnoreCase));
        var daily = player.GetComponent<DailyActiveManager>();
        var accepted = await daily.RollMissions(force);
        var board = daily.GetMissionBoard();
        var diagnostics = I18NManager.Translate("Game.Command.Daily.MissionBoard", board.Roster.ToString(),
            board.GateSatisfied.ToString(), board.Running.ToString(), board.Finished.ToString(),
            board.Offered.ToString(), board.Quota.ToString());

        await arg.SendMsg(accepted.Count > 0
            ? I18NManager.Translate("Game.Command.Daily.MissionOffered", string.Join(",", accepted)) + "\n" + diagnostics
            : I18NManager.Translate("Game.Command.Daily.MissionNone") + "\n" + diagnostics);
    }

    private static async ValueTask<PlayerInstance?> GetPlayer(CommandArg arg)
    {
        var player = arg.Target?.Player;
        if (player?.GetComponentOrDefault<DailyActiveManager>() != null) return player;
        await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
        return null;
    }
}
