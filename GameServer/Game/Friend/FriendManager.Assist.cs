using March7thHoney.Database;
using March7thHoney.Database.Avatar;
using March7thHoney.Database.Inventory;
using March7thHoney.Database.Player;
using March7thHoney.GameServer.Server;
using March7thHoney.GameServer.Server.Packet.Send.Friend;
using March7thHoney.GameServer.Server.Packet.Send.Lineup;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.Friend;

/// <summary>
///     Support characters: building PlayerAssistInfo rows, the support list, and picking a support into the current
///     lineup. Assist slots store the owner's base avatar id plus AssistUid; the wire always carries the path id.
/// </summary>
public partial class FriendManager
{
    public const int MaxPublishedAssists = 3;
    private const int MaxFriendRows = 15;
    private const int MaxStrangerRows = 35;
    private const int MaxAssistRows = 50;

    // Stranger pool for GetAssistList; session-only, re-rolled on an explicit refresh.
    private List<int>? _assistCatalog;

    /// <summary>
    ///     The owner's published support row. avatar_id is the path id, enhanced_id the path's EnhanceId only when
    ///     set, avatar_type 3: a path id in enhanced_id (8001/8002) made the client's team page throw.
    /// </summary>
    public static PlayerAssistInfo? BuildAssistInfo(int ownerUid, int avatarId, FriendOnlineStatus? status = null)
    {
        var owner = PlayerData.GetPlayerByUid(ownerUid);
        var avatars = DatabaseHelper.Instance?.GetInstance<AvatarData>(ownerUid);
        var avatar = avatars?.FindByAnyId(avatarId);
        if (owner == null || avatars == null || avatar == null) return null;

        var inventory = DatabaseHelper.Instance?.GetInstance<InventoryData>(ownerUid) ?? new InventoryData();
        var detail = avatar.ToDetailProto(0, new PlayerDataCollection(owner, inventory, new Database.Lineup.LineupInfo()));
        detail.AvatarType = (uint)AvatarType.AvatarFormalType;
        var enhance = avatar.GetCurPathInfo().EnhanceId;
        if (enhance != 0) detail.EnhancedId = (uint)enhance;

        var online = status ?? (Listener.GetActiveConnection(ownerUid) != null
            ? FriendOnlineStatus.Online
            : FriendOnlineStatus.Offline);
        return new PlayerAssistInfo
        {
            AssistAvatar = owner.ToSimpleProto(online),
            PlayerInfo = detail
        };
    }

    private static List<int> PublishedAssists(int uid)
    {
        return DatabaseHelper.Instance?.GetInstance<AvatarData>(uid)?.AssistAvatars.Where(id => id != 0).ToList() ?? [];
    }

    /// <summary>The current lineup's support slot, if any.</summary>
    public Database.Lineup.LineupAvatarInfo? CurrentAssistSlot()
    {
        return Player.LineupManager?.GetCurLineup()?.BaseAvatars?.FirstOrDefault(slot => slot.AssistUid != 0);
    }

    /// <summary>The selected support, or an empty row: the client builds the support team member from it.</summary>
    public PlayerAssistInfo CurrentAssistInfo()
    {
        var slot = CurrentAssistSlot();
        return slot == null ? new PlayerAssistInfo() : BuildAssistInfo(slot.AssistUid, slot.BaseAvatarId) ?? new PlayerAssistInfo();
    }

    /// <summary>
    ///     Marked friends first, then a pool of other publishers (online or not), each by their first published
    ///     avatar. Excludes self, friends already listed, blacklisted players and pending applications.
    /// </summary>
    public List<PlayerAssistInfo> BuildAssistList(bool refresh)
    {
        List<PlayerAssistInfo> rows = [];
        HashSet<int> seen = [Player.Uid];

        foreach (var (uid, detail) in FriendData.FriendDetailList.OrderByDescending(pair => pair.Value.IsMark))
        {
            if (rows.Count >= MaxFriendRows) break;
            var published = PublishedAssists(uid);
            if (published.Count == 0 || !seen.Add(uid)) continue;
            if (BuildAssistInfo(uid, published[0]) is { } row) rows.Add(row);
        }

        if (refresh || _assistCatalog == null)
        {
            var excluded = new HashSet<int>(FriendData.FriendDetailList.Keys)
                { Player.Uid };
            excluded.UnionWith(FriendData.BlackList);
            excluded.UnionWith(FriendData.SendApplyList);
            excluded.UnionWith(FriendData.ReceiveApplyList);
            _assistCatalog = (DatabaseHelper.GetAllInstance<AvatarData>() ?? [])
                .Where(data => data.AssistAvatars.Any(id => id != 0) && !excluded.Contains(data.Uid))
                .Select(data => data.Uid)
                .OrderBy(_ => Random.Shared.Next())
                .Take(MaxStrangerRows)
                .ToList();
        }

        foreach (var uid in _assistCatalog)
        {
            if (rows.Count >= MaxAssistRows) break;
            var published = PublishedAssists(uid);
            if (published.Count == 0 || !seen.Add(uid)) continue;
            if (BuildAssistInfo(uid, published[0]) is { } row) rows.Add(row);
        }

        // The support already in the team is always listed, so the client can show it selected.
        if (CurrentAssistSlot() is { } slot && seen.Add(slot.AssistUid) &&
            BuildAssistInfo(slot.AssistUid, slot.BaseAvatarId) is { } current)
            rows.Add(current);

        return rows;
    }

    /// <summary>Friends only, one row per published avatar; a friend target_side restricts the list to that uid.</summary>
    public List<PlayerAssistInfo> BuildFriendAssistList(int targetSide)
    {
        List<PlayerAssistInfo> rows = [];
        var uids = targetSide != 0 && FriendData.FriendDetailList.ContainsKey(targetSide)
            ? [targetSide]
            : FriendData.FriendDetailList.Keys.ToList();
        foreach (var uid in uids)
        foreach (var avatarId in PublishedAssists(uid))
            if (BuildAssistInfo(uid, avatarId) is { } row)
                rows.Add(row);
        return rows;
    }

    /// <summary>
    ///     Picks (or with uid/avatar 0 clears) the support in the current lineup. Order on the wire:
    ///     CurAssistChangedNotify, SyncLineupNotify, then the caller's SetAssistScRsp.
    /// </summary>
    public async ValueTask<Retcode> SetAssist(int ownerUid, int avatarId)
    {
        var lineupManager = Player.LineupManager!;
        var lineup = lineupManager.GetCurLineup();
        if (lineup?.BaseAvatars == null) return Retcode.RetFail;

        if (ownerUid == 0 || avatarId == 0)
        {
            lineupManager.ClearAssistSlots();
            await Player.SendPacket(new PacketCurAssistChangedNotify(new PlayerAssistInfo()));
            await Player.SendPacket(new PacketSyncLineupNotify(lineup));
            return Retcode.RetSucc;
        }

        if (ownerUid == Player.Uid) return Retcode.RetAssistNotExist;
        var ownerAvatars = DatabaseHelper.Instance?.GetInstance<AvatarData>(ownerUid);
        var avatar = ownerAvatars?.FindByAnyId(avatarId);
        if (avatar == null || !ownerAvatars!.AssistAvatars.Contains(avatar.BaseAvatarId))
            return Retcode.RetAssistNotExist;
        if (!lineupManager.SetAssistSlot(ownerUid, avatar.BaseAvatarId)) return Retcode.RetFail;

        await Player.SendPacket(new PacketCurAssistChangedNotify(
            BuildAssistInfo(ownerUid, avatar.BaseAvatarId) ?? new PlayerAssistInfo()));
        await Player.SendPacket(new PacketSyncLineupNotify(lineup));
        return Retcode.RetSucc;
    }
}
