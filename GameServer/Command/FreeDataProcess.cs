using March7thHoney.Data;
using March7thHoney.Database;
using March7thHoney.Database.Avatar;
using March7thHoney.Database.Inventory;
using March7thHoney.Enums.Item;
using March7thHoney.GameServer.Server.Packet.Send.PlayerSync;
using March7thHoney.Internationalization;
using March7thHoney.Util;
using System.Runtime.Serialization.Formatters;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace March7thHoney.GameServer.Command
{
    public class FreeDataProcess
    {
        public sealed class FreeSrData
        {
            [JsonPropertyName("relics")] public List<RelicJson>? Relics { get; set; }
            [JsonPropertyName("lightcones")] public List<LightconeJson>? Lightcones { get; set; }
            [JsonPropertyName("avatars")] public Dictionary<int, AvatarJson>? Avatars { get; set; }
        }

        public sealed class RelicJson
        {
            [JsonPropertyName("level")] public int Level { get; set; }
            [JsonPropertyName("relic_id")] public int RelicId { get; set; }
            [JsonPropertyName("main_affix_id")] public int MainAffixId { get; set; }
            [JsonPropertyName("equip_avatar")] public int EquipAvatar { get; set; }
            [JsonPropertyName("sub_affixes")] public List<RelicSubAffixJson>? SubAffixes { get; set; }
        }

        public sealed class RelicSubAffixJson
        {
            [JsonPropertyName("sub_affix_id")] public int SubAffixId { get; set; }
            [JsonPropertyName("count")] public int Count { get; set; }
            [JsonPropertyName("step")] public int Step { get; set; }
        }

        public sealed class LightconeJson
        {
            [JsonPropertyName("level")] public int Level { get; set; }
            [JsonPropertyName("equip_avatar")] public int EquipAvatar { get; set; }
            [JsonPropertyName("item_id")] public int ItemId { get; set; }
            [JsonPropertyName("rank")] public int Rank { get; set; }
            [JsonPropertyName("promotion")] public int Promotion { get; set; }
        }

        public sealed class AvatarJson
        {
            [JsonPropertyName("avatar_id")] public int AvatarId { get; set; }
            [JsonPropertyName("level")] public int Level { get; set; }
            [JsonPropertyName("promotion")] public int Promotion { get; set; }
            [JsonPropertyName("data")] public AvatarExtraJson? Data { get; set; }
        }

        public sealed class AvatarExtraJson
        {
            [JsonPropertyName("rank")] public int Rank { get; set; }
            [JsonPropertyName("skills")] public Dictionary<int, int>? Skills { get; set; }
        }

        public static readonly JsonSerializerOptions JsonReadOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public static async ValueTask<List<FormalAvatarInfo>> ImportAvatars(March7thHoney.GameServer.Game.Player.PlayerInstance player,
            FreeSrData data, Func<string[], Task> SendI18nCallback)
        {
            var changed = new Dictionary<int, FormalAvatarInfo>();

            if (data.Avatars == null || data.Avatars.Count == 0) return [];

            foreach (var (avatarKey, avatarJson) in data.Avatars)
            {
                var avatarId = avatarJson.AvatarId > 0 ? avatarJson.AvatarId : avatarKey;
                var baseAvatarId = GameData.MultiplePathAvatarConfigData.TryGetValue(avatarId, out var multiplePath)
                    ? multiplePath.BaseAvatarID
                    : avatarId;

                if (!GameData.AvatarConfigData.ContainsKey(avatarId))
                {
                    await SendI18nCallback(new string[] { "Game.Command.Json.AvatarExcelNotFound", avatarId.ToString() });
                    continue;
                }

                if (player.AvatarManager?.GetFormalAvatar(baseAvatarId) == null)
                {
                    await player.InventoryManager!.AddItem(baseAvatarId, 1, notify: false, sync: false);
                }

                var avatar = player.AvatarManager?.GetFormalAvatar(baseAvatarId);
                if (avatar == null) continue;
                if (!avatar.PathInfos.ContainsKey(avatarId))
                {
                    avatar.PathInfos[avatarId] = new PathInfo(avatarId);
                    avatar.PathInfos[avatarId].GetSkillTree();
                }

                avatar.Level = Math.Clamp(avatarJson.Level, 1, 80);
                avatar.Promotion = avatarJson.Promotion > 0
                    ? Math.Clamp(avatarJson.Promotion, 0, 6)
                    : GameData.GetMinPromotionForLevel(avatar.Level);

                var pathInfo = avatar.PathInfos[avatarId];
                pathInfo.Rank = Math.Clamp(avatarJson.Data?.Rank ?? 0, 0, 6);

                // skills: pointId -> level
                if (avatarJson.Data?.Skills != null)
                {
                    var skillTree = pathInfo.GetSkillTree();
                    skillTree.Clear();
                    foreach (var (pointId, level) in avatarJson.Data.Skills)
                        skillTree[pointId] = Math.Max(1, level);
                }

                changed[avatar.BaseAvatarId] = avatar;
            }

            return [.. changed.Values];
        }

        public static async ValueTask<List<ItemData>> ImportRelicsAndLightcones(
            March7thHoney.GameServer.Game.Player.PlayerInstance player,
            FreeSrData data,
            List<FormalAvatarInfo> avatarChanged)
        {
            var importedItems = new List<ItemData>(Math.Max(16, (data.Relics?.Count ?? 0) + (data.Lightcones?.Count ?? 0)));
            var avatarChangedMap = avatarChanged.ToDictionary(x => x.BaseAvatarId, x => x);

            FormalAvatarInfo? GetAvatar(int pathOrBaseAvatarId)
            {
                var baseAvatarId = GameData.MultiplePathAvatarConfigData.TryGetValue(pathOrBaseAvatarId, out var multiPath)
                    ? multiPath.BaseAvatarID
                    : pathOrBaseAvatarId;

                if (avatarChangedMap.TryGetValue(baseAvatarId, out var existing)) return existing;
                var avatar = player.AvatarManager?.GetFormalAvatar(baseAvatarId);
                if (avatar == null) return null;
                avatarChangedMap[baseAvatarId] = avatar;
                return avatar;
            }

            void EnsurePath(FormalAvatarInfo avatar, int avatarId)
            {
                if (!avatar.PathInfos.ContainsKey(avatarId))
                {
                    avatar.PathInfos[avatarId] = new PathInfo(avatarId);
                    avatar.PathInfos[avatarId].GetSkillTree();
                }
            }

            if (data.Relics != null)
            {
                foreach (var relic in data.Relics)
                {
                    if (!GameData.RelicConfigData.TryGetValue(relic.RelicId, out var relicConfig)) continue;
                    if (!GameData.ItemConfigData.TryGetValue(relic.RelicId, out var itemConfig) ||
                        itemConfig.ItemMainType != ItemMainTypeEnum.Relic)
                        continue;
                    if (!GameData.RelicMainAffixData.TryGetValue(relicConfig.MainAffixGroup, out var mainAffixGroup) ||
                        mainAffixGroup.Count == 0)
                        continue;

                    var subAffixes = new List<ItemSubAffix>(relic.SubAffixes?.Count ?? 0);
                    if (relic.SubAffixes != null &&
                        GameData.RelicSubAffixData.TryGetValue(relicConfig.SubAffixGroup, out var subGroup) &&
                        subGroup != null)
                        foreach (var sub in relic.SubAffixes)
                        {
                            if (!subGroup.ContainsKey(sub.SubAffixId)) continue;
                            subAffixes.Add(new ItemSubAffix
                            {
                                Id = sub.SubAffixId,
                                Count = Math.Max(1, sub.Count),
                                Step = Math.Max(0, sub.Step)
                            });
                        }

                    var mainAffixId = mainAffixGroup.ContainsKey(relic.MainAffixId)
                        ? relic.MainAffixId
                        : mainAffixGroup.Keys.First();

                    var item = await player.InventoryManager!.PutItem(
                        relic.RelicId,
                        1,
                        level: Math.Clamp(relic.Level, 0, relicConfig.MaxLevel),
                        mainAffix: mainAffixId,
                        subAffixes: subAffixes,
                        uniqueId: ++player.InventoryManager.Data.NextUniqueId);

                    importedItems.Add(item);

                    if (relic.EquipAvatar > 0)
                    {
                        var targetPathId = relic.EquipAvatar;
                        if (!GameData.AvatarConfigData.ContainsKey(targetPathId)) continue;

                        var avatar = GetAvatar(targetPathId);
                        if (avatar == null) continue;

                        EnsurePath(avatar, targetPathId);
                        var slot = (int)relicConfig.Type;
                        avatar.PathInfos[targetPathId].Relic[slot] = item.UniqueId;
                        item.EquipAvatar = targetPathId;
                    }
                }
            }

            if (data.Lightcones != null)
            {
                foreach (var lightcone in data.Lightcones)
                {
                    if (!GameData.ItemConfigData.TryGetValue(lightcone.ItemId, out var itemConfig) ||
                        itemConfig.ItemMainType != ItemMainTypeEnum.Equipment)
                        continue;
                    if (!GameData.EquipmentConfigData.TryGetValue(lightcone.ItemId, out var equipmentConfig))
                        continue;

                    var item = await player.InventoryManager!.PutItem(
                        lightcone.ItemId,
                        1,
                        rank: Math.Clamp(lightcone.Rank, 1, Math.Max(1, equipmentConfig.MaxRank)),
                        promotion: Math.Clamp(lightcone.Promotion, 0, Math.Max(0, equipmentConfig.MaxPromotion)),
                        level: Math.Clamp(lightcone.Level, 1, 80),
                        uniqueId: ++player.InventoryManager.Data.NextUniqueId);

                    importedItems.Add(item);

                    if (lightcone.EquipAvatar > 0)
                    {
                        var targetPathId = lightcone.EquipAvatar;
                        if (!GameData.AvatarConfigData.ContainsKey(targetPathId)) continue;

                        var avatar = GetAvatar(targetPathId);
                        if (avatar == null) continue;
                        EnsurePath(avatar, targetPathId);
                        avatar.PathInfos[targetPathId].EquipId = item.UniqueId;
                        item.EquipAvatar = targetPathId;
                    }
                }
            }

            // refresh caller list
            avatarChanged.Clear();
            avatarChanged.AddRange(avatarChangedMap.Values);

            return importedItems;
        }

        public static async ValueTask<(List<FormalAvatarInfo> changedAvatars, List<ItemData> removedItems)> ClearRelicAndEquipment(March7thHoney.GameServer.Game.Player.PlayerInstance player)
        {
            var changed = new Dictionary<int, FormalAvatarInfo>();

            void MarkChanged(FormalAvatarInfo avatar)
            {
                if (!changed.ContainsKey(avatar.AvatarId))
                    changed.Add(avatar.AvatarId, avatar);
            }

            var inv = player.InventoryManager!.Data;

            foreach (var item in inv.EquipmentItems)
            {
                if (item.EquipAvatar <= 0) continue;
                var avatar = player.AvatarManager?.GetFormalAvatar(item.EquipAvatar);
                if (avatar == null) continue;
                var pathInfo = avatar.PathInfos.GetValueOrDefault(item.EquipAvatar)
                               ?? avatar.PathInfos.Values.FirstOrDefault(x => x.EquipId == item.UniqueId);
                if (pathInfo != null && pathInfo.EquipId == item.UniqueId)
                    pathInfo.EquipId = 0;
                item.EquipAvatar = 0;
                MarkChanged(avatar);
            }

            foreach (var item in inv.RelicItems)
            {
                if (item.EquipAvatar <= 0) continue;
                var avatar = player.AvatarManager?.GetFormalAvatar(item.EquipAvatar);
                if (avatar == null) continue;
                var pathInfo = avatar.PathInfos.GetValueOrDefault(item.EquipAvatar)
                               ?? avatar.PathInfos.Values.FirstOrDefault(x => x.Relic.Values.Contains(item.UniqueId));
                if (pathInfo != null)
                {
                    var toRemoveSlots = pathInfo.Relic.Where(kv => kv.Value == item.UniqueId).Select(kv => kv.Key).ToList();
                    foreach (var slot in toRemoveSlots) pathInfo.Relic.Remove(slot);
                }

                item.EquipAvatar = 0;
                MarkChanged(avatar);
            }

            var toRemove = new List<(int itemId, int count, int uniqueId)>(inv.EquipmentItems.Count + inv.RelicItems.Count);
            toRemove.AddRange(inv.EquipmentItems.Select(x => (x.ItemId, 1, x.UniqueId)));
            toRemove.AddRange(inv.RelicItems.Select(x => (x.ItemId, 1, x.UniqueId)));

            // Remove without syncing; caller sends a single batch sync.
            var removed = await player.InventoryManager.RemoveItems(toRemove, sync: false);

            return ([.. changed.Values], removed);
        }

        public static async ValueTask ImportJson(string json, Game.Player.PlayerInstance player, Func<string[], Task> SendI18nCallback)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            var data = System.Text.Json.JsonSerializer.Deserialize<FreeDataProcess.FreeSrData>(json, options);

            if (data == null)
            {
                await SendI18nCallback(new string[] { "Game.Command.Json.InvalidJsonContent" });
                return;
            }

            var (clearedAvatars, clearedItems) = await FreeDataProcess.ClearRelicAndEquipment(player);
            if (clearedAvatars.Count > 0)
                await player.SendPacket(new PacketPlayerSyncScNotify(clearedAvatars));
            if (clearedItems.Count > 0)
                await player.SendPacket(new PacketPlayerSyncScNotify(clearedItems));

            var avatarChanged = await FreeDataProcess.ImportAvatars(player, data, async (msg) => await SendI18nCallback(msg));
            var importedItems = await FreeDataProcess.ImportRelicsAndLightcones(player, data, avatarChanged);

            if (importedItems.Count > 0)
                await player.SendPacket(new PacketPlayerSyncScNotify(importedItems));
            if (avatarChanged.Count > 0)
                await player.SendPacket(new PacketPlayerSyncScNotify(avatarChanged));

            DatabaseHelper.ToSaveUidList.SafeAdd(player.Uid);

            await SendI18nCallback(new string[] { "Game.Command.Json.ImportSummary", (data.Avatars?.Count ?? 0).ToString(), (data.Relics?.Count ?? 0).ToString(), (data.Lightcones?.Count ?? 0).ToString()});
        }


        public static async ValueTask<string> ExportPlayerDataAsync(int uid, bool geshihua)
        {
            var data = new FreeSrData
            {
                Avatars = new Dictionary<int, AvatarJson>(),
                Relics = new List<RelicJson>(),
                Lightcones = new List<LightconeJson>()
            };

            var dbHelper = DatabaseHelper.Instance;
            if (dbHelper == null)
                throw new Exception("DatabaseHelper not initialized.");

            var avatarData = dbHelper.GetInstance<AvatarData>(uid);
            if (avatarData != null)
            {
                foreach (var avatar in avatarData.FormalAvatars)
                {
                    foreach (var path in avatar.PathInfos.Values)
                    {
                        var avatarJson = new FreeDataProcess.AvatarJson
                        {
                            AvatarId = path.PathId,
                            Level = avatar.Level,
                            Promotion = avatar.Promotion,
                            Data = new FreeDataProcess.AvatarExtraJson
                            {
                                Rank = path.Rank,
                                Skills = path.GetSkillTree()
                                    .ToDictionary(kv => kv.Key, kv => kv.Value)
                            }
                        };

                        data.Avatars[avatarJson.AvatarId] = avatarJson;
                    }
                }
            }

            var inventoryData = dbHelper.GetInstance<InventoryData>(uid);
            if (inventoryData != null)
            {
                foreach (var relic in inventoryData.RelicItems)
                {
                    var relicJson = new FreeDataProcess.RelicJson
                    {
                        Level = relic.Level,
                        RelicId = relic.ItemId,
                        MainAffixId = relic.MainAffix,
                        EquipAvatar = relic.EquipAvatar,
                        SubAffixes = relic.SubAffixes?.Select(sa => new RelicSubAffixJson
                        {
                            SubAffixId = sa.Id,
                            Count = sa.Count,
                            Step = sa.Step
                        }).ToList()
                    };
                    data.Relics.Add(relicJson);
                }

                foreach (var equip in inventoryData.EquipmentItems)
                {
                    var lightconeJson = new FreeDataProcess.LightconeJson
                    {
                        ItemId = equip.ItemId,
                        Level = equip.Level,
                        Rank = equip.Rank,
                        Promotion = equip.Promotion,
                        EquipAvatar = equip.EquipAvatar
                    };
                    data.Lightcones.Add(lightconeJson);
                }
            }

            return await Task.FromResult(JsonSerializer.Serialize(
                data,
                new JsonSerializerOptions
                {
                    WriteIndented = geshihua,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                }
            ));
        }
    }
}
