using March7thHoney.GameServer.Game.GridFight;
using March7thHoney.GameServer.Server.Packet.Send.GridFight;
using March7thHoney.Internationalization;

namespace March7thHoney.Command.Command.Cmd;

[CommandInfo("grid", "Game.Command.Grid.Desc", "Game.Command.Grid.Usage", permission: CommandPermissions.Grid)]
public sealed class CommandGrid : ICommand
{
    [CommandMethod("0 gold")]
    public async ValueTask UpdateGold(CommandArg arg)
    {
        var player = arg.Target?.Player;
        if (player == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        var manager = player.GridFightManager;
        if (manager == null || manager.Session is not { } session)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.NotInGame"));
            return;
        }

        if (!arg.TryGetInt(0, out var delta))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        session.AdjustGold(delta);
        await player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(manager.BuildGoldSync()));
        await SendSeasonTwoFollowUp(player, manager);
        await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.UpdateGold", delta.ToString()));
    }

    [CommandMethod("0 role")]
    public async ValueTask AddRole(CommandArg arg)
    {
        var player = arg.Target?.Player;
        if (player == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        var manager = player.GridFightManager;
        if (manager == null || manager.Session is not { } session)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.NotInGame"));
            return;
        }

        if (!arg.TryGetInt(0, out var roleId) || !arg.TryGetInt(1, out var roleStar))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        if (!session.TryAddRole((uint)roleId, (uint)Math.Max(1, roleStar), out var acquisition))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.InvalidRole"));
            return;
        }

        await player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(
            manager.BuildRoleAcquisitionSync(acquisition)));
        await SendSeasonTwoFollowUp(player, manager);
        await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.AddedRole"));
    }

    [CommandMethod("0 equip")]
    public async ValueTask AddEquipment(CommandArg arg)
    {
        var player = arg.Target?.Player;
        if (player == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        var manager = player.GridFightManager;
        if (manager == null || manager.Session is not { } session)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.NotInGame"));
            return;
        }

        if (!arg.TryGetInt(0, out var equipmentId))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        if (!session.TryAddEquipment((uint)equipmentId, out var equipment))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.InvalidEquipment"));
            return;
        }

        await player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(
            manager.BuildEquipmentAddedSync(equipment)));
        await SendSeasonTwoFollowUp(player, manager);
        await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.AddEquipment", equipmentId.ToString()));
    }

    [CommandMethod("0 orb")]
    public async ValueTask AddOrb(CommandArg arg)
    {
        var player = arg.Target?.Player;
        if (player == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        var manager = player.GridFightManager;
        if (manager == null || manager.Session is not { } session)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.NotInGame"));
            return;
        }

        if (!arg.TryGetInt(0, out var orbItemId))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        if (!session.TryAddOrb((uint)orbItemId, out var orb))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.InvalidOrb"));
            return;
        }

        await player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(manager.BuildOrbAddedSync(orb)));
        await SendSeasonTwoFollowUp(player, manager);
        await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.AddOrb", orbItemId.ToString()));
    }

    [CommandMethod("0 consumable")]
    public async ValueTask AddConsumable(CommandArg arg)
    {
        var player = arg.Target?.Player;
        if (player == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        var manager = player.GridFightManager;
        if (manager == null || manager.Session is not { } session)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.NotInGame"));
            return;
        }

        if (!arg.TryGetInt(0, out var consumableId))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        if (!session.TryAddConsumable((uint)consumableId, out var change))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.InvalidConsumable"));
            return;
        }

        await player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(
            manager.BuildConsumableSync(change)));
        await SendSeasonTwoFollowUp(player, manager);
        await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.AddConsumable", consumableId.ToString()));
    }

    [CommandMethod("0 section")]
    public async ValueTask SetSection(CommandArg arg)
    {
        var player = arg.Target?.Player;
        if (player == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        var manager = player.GridFightManager;
        if (manager == null || manager.Session is not { } session)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.NotInGame"));
            return;
        }

        if (!arg.TryGetInt(0, out var chapterId) || !arg.TryGetInt(1, out var sectionId))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        var chapter = (uint)Math.Max(1, chapterId);
        var section = (uint)Math.Max(1, sectionId);
        if (!session.TryMoveToSection(chapter, section, out var finishedPendingPosition))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        await player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(
            manager.BuildSectionMoveSync(finishedPendingPosition)));
        await SendSeasonTwoFollowUp(player, manager);
        await arg.SendMsg(I18NManager.Translate(
            "Game.Command.Grid.EnterSection",
            chapter.ToString(),
            section.ToString()));
    }

    [CommandMethod("0 exp")]
    public ValueTask AddExp(CommandArg arg) =>
        ApplyGrant(arg, amount => amount > 0 ? new GridFightAugmentGrant { Exp = (uint)amount } : null, "Exp");

    [CommandMethod("0 hp")]
    public ValueTask AdjustHp(CommandArg arg) =>
        ApplyGrant(arg, amount => amount switch
        {
            > 0 => new GridFightAugmentGrant { Hp = (uint)amount },
            < 0 => new GridFightAugmentGrant { HpLoss = (uint)-amount },
            _ => null,
        }, "Hp");

    [CommandMethod("0 maxhp")]
    public ValueTask AddMaxHp(CommandArg arg) =>
        ApplyGrant(arg, amount => amount > 0 ? new GridFightAugmentGrant { MaxHp = (uint)amount } : null, "MaxHp");

    [CommandMethod("0 refresh")]
    public ValueTask AddFreeRefresh(CommandArg arg) =>
        ApplyGrant(arg, amount => amount > 0 ? new GridFightAugmentGrant { FreeRefresh = (uint)amount } : null, "Refresh");

    [CommandMethod("0 emblem")]
    public ValueTask AddEmblems(CommandArg arg) =>
        ApplyGrant(arg, amount => amount is > 0 and <= 20 ? new GridFightAugmentGrant { Emblems = (uint)amount } : null, "Emblem");

    [CommandMethod("0 streak")]
    public ValueTask AddWinStreak(CommandArg arg) =>
        ApplyGrant(arg, amount => amount > 0 ? new GridFightAugmentGrant { WinStreak = (uint)amount } : null, "Streak");

    [CommandMethod("0 augment")]
    public async ValueTask AddAugment(CommandArg arg)
    {
        if (await ResolveSession(arg) is not var (player, manager, session)) return;
        if (!arg.TryGetInt(0, out var augmentId) || augmentId <= 0)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        if (!session.TryGrantInvestmentByCommand((uint)augmentId))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.InvalidAugment", augmentId.ToString()));
            return;
        }

        await player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(manager.BuildCommandInvestmentSync((uint)augmentId)));
        await SendSeasonTwoFollowUp(player, manager);
        await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.AddedAugment", augmentId.ToString()));
    }

    [CommandMethod("0 trailblaze")]
    public async ValueTask AdjustTrailblaze(CommandArg arg)
    {
        if (await ResolveSession(arg) is not var (player, manager, session)) return;
        if (!arg.TryGetInt(0, out var delta) || delta == 0)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        var coin = session.AdjustTrailblazeCoin(delta);
        await SendSeasonTwoFollowUp(player, manager);
        await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.UpdateTrailblaze", coin.ToString()));
    }

    [CommandMethod("0 info")]
    public async ValueTask ShowInfo(CommandArg arg)
    {
        if (await ResolveSession(arg) is not var (_, _, session)) return;
        await arg.SendMsg(session.DescribeForCommand());
    }

    /// <summary>Applies an investment-style grant (gold/exp/hp/refresh/emblems/streak) through the normal reward sync.</summary>
    private static async ValueTask ApplyGrant(CommandArg arg, Func<int, GridFightAugmentGrant?> build, string kind)
    {
        if (await ResolveSession(arg) is not var (player, manager, session)) return;
        if (!arg.TryGetInt(0, out var amount) || build(amount) is not { } grant)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        var effect = session.ApplyCommandGrant(grant);
        if (!effect.Application.Success)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.GrantFailed"));
            return;
        }

        await player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(manager.BuildCommandGrantSync(effect)));
        if (grant.FreeRefresh > 0)
            await player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(manager.BuildCommandShopSync()));
        await SendSeasonTwoFollowUp(player, manager);
        await arg.SendMsg(I18NManager.Translate($"Game.Command.Grid.Granted{kind}", amount.ToString()));
    }

    private static async ValueTask<(March7thHoney.GameServer.Game.Player.PlayerInstance Player, GridFightManager Manager,
        GridFightSession Session)?> ResolveSession(CommandArg arg)
    {
        var player = arg.Target?.Player;
        if (player == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return null;
        }

        if (player.GridFightManager is not { Session: { } session } manager)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.NotInGame"));
            return null;
        }

        return (player, manager, session);
    }

    /// <summary>
    ///     Season-2 trait state (Tenglong summon, Aha growth, trait prompts...) follows board changes; client
    ///     requests flush it in GridFightHandler, console commands have to do the same. Every state-changing
    ///     subcommand ends here, so this is also where the changed run is saved.
    /// </summary>
    private static async ValueTask SendSeasonTwoFollowUp(
        March7thHoney.GameServer.Game.Player.PlayerInstance player,
        GridFightManager manager)
    {
        var followUp = manager.BuildSeasonTwoFollowUpSync();
        manager.Persist();
        if (followUp != null)
            await player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(followUp));
    }
}
