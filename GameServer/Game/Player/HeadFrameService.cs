using March7thHoney.Data;
using March7thHoney.Database;
using March7thHoney.Enums.Item;

namespace March7thHoney.GameServer.Game.Player;

/// <summary>
///     头像框的拥有与展示。
///     <para>
///         拥有: 客户端 <c>HeadFrameUtils.HasHeadFrame</c> 按背包里是否有子类型 HeadIconFrame 的物品判断,
///         所以发放头像框时物品也要进背包(每种一个)。
///     </para>
///     <para>
///         展示: 玩家设置 <see cref="Database.Player.PrivacySettingsPb.HideHeadFrame" />, 客户端
///         <c>PlayerSettingData.IsDisplayHeadFrame</c> = !该值。隐藏只影响其他玩家看到的资料
///         (<see cref="Database.Player.PlayerData.ToPublicHeadFrameProto" />), 不收回、不替换已佩戴的头像框。
///     </para>
/// </summary>
public static class HeadFrameService
{
    public static bool IsHeadFrameItem(uint itemId)
    {
        return GameData.ItemConfigData.GetValueOrDefault((int)itemId)?.ItemSubType == ItemSubTypeEnum.HeadIconFrame;
    }

    /// <summary>发放并佩戴头像框: 物品放入背包(已有则不重复), 再设为当前头像框; 不改动玩家的展示设置。</summary>
    public static async ValueTask Grant(PlayerInstance player, uint frameId, long expireTime)
    {
        await EnsureOwned(player, frameId, true);
        await player.SetPlayerHeadFrameId(frameId, expireTime);
    }

    /// <summary>
    ///     登录修复: 旧逻辑只写头像框信息、不放物品, 这类存档在背包补上当前头像框后客户端才会出现展示开关。
    ///     登录后客户端会重新拉背包, 这里不单独同步。
    /// </summary>
    public static async ValueTask RepairOwnedItem(PlayerInstance player)
    {
        await EnsureOwned(player, player.Data.HeadFrame.HeadFrameId, false);
    }

    /// <summary>UpdatePlayerSetting oneof 字段 1600: true 隐藏, false 展示。</summary>
    public static void SetHidden(PlayerInstance player, bool hidden)
    {
        var settings = player.Data.PrivacySettings;
        if (settings.HideHeadFrame == hidden) return;
        settings.HideHeadFrame = hidden;
        DatabaseHelper.MarkDirty(player.Uid);
    }

    private static async ValueTask EnsureOwned(PlayerInstance player, uint frameId, bool sync)
    {
        if (!IsHeadFrameItem(frameId) || player.InventoryManager!.GetItem((int)frameId) != null) return;
        await player.InventoryManager.AddItem((int)frameId, 1, false, sync: sync);
    }
}
