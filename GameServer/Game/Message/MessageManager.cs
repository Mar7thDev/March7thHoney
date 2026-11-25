using March7thHoney.Data;
using March7thHoney.Database;
using March7thHoney.Database.Message;
using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server.Packet.Send.PlayerSync;
using March7thHoney.Proto;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Game.Message;

public class MessageManager(PlayerInstance player) : BasePlayerManager<MessageData>(player)
{
    private static readonly Logger Logger = new("Message");

    public List<MessageSectionData> PendingMessagePerformSectionList { get; private set; } = [];

    #region Get

    public MessageSectionData? GetMessageSectionData(int sectionId)
    {
        GameData.MessageSectionConfigData.TryGetValue(sectionId, out var sectionConfig);
        if (sectionConfig == null) return null;
        var groupId = sectionConfig.GroupID;
        if (!Data.Groups.TryGetValue(groupId, out var group)) return null;
        return group.Sections.FirstOrDefault(m => m.SectionId == sectionId);
    }

    public List<MessageGroup> GetMessageGroup(int contactId)
    {
        GameData.MessageContactsConfigData.TryGetValue(contactId, out var contactConfig);
        if (contactConfig == null) return [];
        var stubFinishUnseen = !Player.MissionEnabled;
        var result = new List<MessageGroup>();
        foreach (var item in contactConfig.Groups)
            if (Data.Groups.TryGetValue(item.ID, out var group))
            {
                var groupInfo = new MessageGroup
                {
                    Id = (uint)item.ID,
                    Status = group.Status,
                    RefreshTime = group.RefreshTime
                };
                foreach (var section in group.Sections)
                {
                    var sectionInfo = new MessageSection
                    {
                        Id = (uint)section.SectionId,
                        Status = section.Status
                    };
                    // Official wire shape (GetNpcMessageGroupScRsp): only a DOING section carries its offered
                    // list - shipping it for a finished section kept those bubbles selectable, so chats looked
                    // un-finished / re-opened.
                    if (section.Status == MessageSectionStatus.MessageSectionDoing)
                        sectionInfo.MessageItemList.AddRange(section.ToChooseItemId.Select(m => (uint)m));
                    // The history is every item the client confirmed, in confirmation order: official sections
                    // carry up to nine rows, and echoing only the last one left a multi-bubble chat showing a
                    // single bubble. A save written before the confirmed list existed falls back to its last item.
                    if (section.ConfirmedItemIds.Count > 0)
                        sectionInfo.ItemList.AddRange(section.ConfirmedItemIds.Select(id => new MessageItem
                        {
                            ItemId = (uint)id
                        }));
                    else if (section.Items.Count > 0)
                        sectionInfo.ItemList.Add(new MessageItem
                        {
                            ItemId = (uint)section.Items[^1].ItemId
                        });
                    groupInfo.MessageSectionList.Add(sectionInfo);
                }

                // A finished group names no current section: official captures send 0, and a closed chat that still
                // pointed at a section looked like it was running.
                groupInfo.MessageSectionId = group.Status == MessageGroupStatus.MessageGroupFinish
                    ? 0
                    : (uint)group.CurrentSectionId;
                result.Add(groupInfo);
            }
            else if (stubFinishUnseen)
            {
                // Without missions, unseen message groups are unreachable. Report them as finished for client
                // unlock checks such as Forgotten Hall (Pom-Pom messages 4010105/4010134).
                var stub = new MessageGroup
                {
                    Id = (uint)item.ID,
                    Status = MessageGroupStatus.MessageGroupFinish,
                    RefreshTime = Extensions.GetUnixSec()
                };
                if (GameData.MessageGroupConfigData.TryGetValue(item.ID, out var groupConfig))
                {
                    foreach (var sid in groupConfig.MessageSectionIDList)
                        stub.MessageSectionList.Add(new MessageSection
                        {
                            Id = (uint)sid,
                            Status = MessageSectionStatus.MessageSectionFinish
                        });
                    if (groupConfig.MessageSectionIDList.Count > 0)
                        stub.MessageSectionId = (uint)groupConfig.MessageSectionIDList[^1];
                }
                result.Add(stub);
            }
            // Unstarted groups are NOT serialized: the official server ships only groups with real player
            // state (4 groups / 112 bytes on the prologue capture) and the client resolves the rest from its
            // own config. Sending a None row per config group pre-rendered every chat as already sent.

        return result;
    }

    #endregion

    #region Action

    /// <summary>Starts a section from its opening items; a section the player already has is left alone.</summary>
    /// <returns>Whether the section was started by this call.</returns>
    public async ValueTask<bool> AddMessageSection(int sectionId, bool sendPacket = true)
    {
        GameData.MessageSectionConfigData.TryGetValue(sectionId, out var sectionConfig);
        if (sectionConfig == null) return false;

        if (Data.Groups.TryGetValue(sectionConfig.GroupID, out var group) &&
            group.Sections.Find(x => x.SectionId == sectionId) != null)
            // already exist
            return false;

        foreach (var item in sectionConfig.StartMessageItemIDList) await AddMessageItem(item, sendPacket);
        return GetMessageSectionData(sectionId) != null;
    }

    /// <summary>
    ///     Drops the stored state of one message group. The offer and the history are derived from the config and
    ///     the client's picks, so a section saved in a shape no client can render (for example a mission card in its
    ///     history and nothing offered) cannot be patched in place; the next AddMessageSection rebuilds it.
    /// </summary>
    /// <returns>The sections the group had, empty when the player never started it.</returns>
    public List<int> ResetGroup(int groupId)
    {
        if (!Data.Groups.Remove(groupId, out var group)) return [];
        MarkDirty();
        return group.Sections.Select(section => section.SectionId).ToList();
    }

    public async ValueTask AddMessageItem(int itemId, bool sendPacket = true)
    {
        GameData.MessageItemConfigData.TryGetValue(itemId, out var itemConfig);
        if (itemConfig == null) return;
        var groupId = itemConfig.GroupID;
        var sectionId = itemConfig.SectionID;
        if (!Data.Groups.TryGetValue(groupId, out var group))
        {
            group = new MessageGroupData
            {
                GroupId = groupId,
                CurrentSectionId = sectionId,
                RefreshTime = Extensions.GetUnixSec(),
                Status = MessageGroupStatus.MessageGroupDoing
            };
            var section = new MessageSectionData
            {
                SectionId = sectionId,
                Status = MessageSectionStatus.MessageSectionDoing,
                ToChooseItemId = [.. itemConfig.NextItemIDList]
            };
            // A chat that opens a new group plays up to its first reply as well: section 1200000 has to offer the
            // two answers after "Message failed to send" (official: group 12000 offers 120000002/120000003), not
            // the System line the client never picks.
            AutoAdvance(section);
            group.Sections.Add(section);
            Data.Groups.Add(groupId, group);
        }
        else
        {
            group.CurrentSectionId = sectionId;
            group.RefreshTime = Extensions.GetUnixSec();
            group.Status = MessageGroupStatus.MessageGroupDoing;
            if (group.Sections.All(m => m.SectionId != sectionId)) // new section
            {
                var section = new MessageSectionData
                {
                    SectionId = sectionId,
                    Status = MessageSectionStatus.MessageSectionDoing,
                    ToChooseItemId = [.. itemConfig.NextItemIDList]
                };
                AutoAdvance(section);
                group.Sections.Add(section);
            }
            else // old
            {
                group.Sections.First(m => m.SectionId == sectionId).Status = MessageSectionStatus.MessageSectionDoing;
            }
        }

        MarkDirty();
        // sync
        if (sendPacket)
        {
            var notify = new PacketPlayerSyncScNotify(group, group.Sections.First(m => m.SectionId == sectionId));
            await Player.SendPacket(notify);
        }
    }

    public async ValueTask FinishSection(int sectionId, bool sendPacket = true)
    {
        GameData.MessageSectionConfigData.TryGetValue(sectionId, out var sectionConfig);
        if (sectionConfig == null) return;
        var groupId = sectionConfig.GroupID;
        if (!Data.Groups.TryGetValue(groupId, out var group)) return;
        var section = group.Sections.Find(m => m.SectionId == sectionId);
        if (section?.Status != MessageSectionStatus.MessageSectionDoing) return;
        section.Status = MessageSectionStatus.MessageSectionFinish;
        // A section that ends on a mission card moves it from the offer into the history: official finished
        // sections list the card in their history (group 12011: 120110006, 120110002, 120110007) and offer nothing.
        foreach (var offered in section.ToChooseItemId)
            if (GameData.MessageItemConfigData.TryGetValue(offered, out var offeredConfig) &&
                offeredConfig.IsMissionCard())
            {
                section.Items.Add(new MessageItemData { ItemId = offered });
                section.ConfirmedItemIds.Add(offered);
            }

        section.ToChooseItemId = [];
        if (group.Sections.All(m => m.Status == MessageSectionStatus.MessageSectionFinish))
            group.Status = MessageGroupStatus.MessageGroupFinish;

        MarkDirty();
        // sync
        if (sendPacket)
        {
            var notify = new PacketPlayerSyncScNotify(group, section);
            await Player.SendPacket(notify);
        }

        // broadcast to mission system
        await Player.MissionManager!.HandleFinishType(MissionFinishTypeEnum.MessagePerformSectionFinish);
        await Player.MissionManager!.HandleFinishType(MissionFinishTypeEnum.MessageSectionFinish);
    }

    /// <summary>The client picked an offered item; the section then plays its follow-ups up to the next reply.</summary>
    /// <returns>Whether the pick was accepted.</returns>
    public async ValueTask<bool> FinishMessageItem(int itemId, bool sendPacket = true)
    {
        if (!GameData.MessageItemConfigData.TryGetValue(itemId, out var itemConfig) ||
            !Data.Groups.TryGetValue(itemConfig.GroupID, out var group))
            return false;
        var section = group.Sections.Find(m => m.SectionId == itemConfig.SectionID);
        if (section?.Status != MessageSectionStatus.MessageSectionDoing) return false;
        // Reject client ids that aren't currently offered — stops arbitrary message-flow advancement.
        if (!section.ToChooseItemId.Contains(itemId))
        {
            Logger.Debug($"uid {Player.Uid} picked message item {itemId}, which section {section.SectionId} " +
                         $"does not offer ([{string.Join(", ", section.ToChooseItemId)}])");
            return false;
        }

        // A mission card stays offered until FinishSection retires it: the offered card is what pins the chat and
        // shows the mission icon, and consuming it emptied the offer while the quest was still running.
        if (itemConfig.IsMissionCard()) return false;

        section.Items.Add(new MessageItemData
        {
            ItemId = itemId
        });
        section.ConfirmedItemIds.Add(itemId);
        // A copy: NextItemIDList belongs to the shared config, not to this player's section.
        section.ToChooseItemId = [.. itemConfig.NextItemIDList];
        AutoAdvance(section);
        group.RefreshTime = Extensions.GetUnixSec();

        MarkDirty();
        if (sendPacket)
        {
            // sync
            var notify = new PacketPlayerSyncScNotify(group, section);
            await Player.SendPacket(notify);
        }

        return true;
    }

    /// <summary>
    ///     Records the items a staged performance reports as picked (FinishPerformSectionIdCsReq): that path sends
    ///     the whole list at once instead of one FinishItemIdCsReq per bubble, and the official history of such a
    ///     section is exactly that list.
    /// </summary>
    public void RecordConfirmedItems(int sectionId, IEnumerable<int> itemIds)
    {
        var section = GetMessageSectionData(sectionId);
        if (section?.Status != MessageSectionStatus.MessageSectionDoing) return;
        var changed = false;
        foreach (var itemId in itemIds)
        {
            if (itemId == 0 || section.ConfirmedItemIds.Contains(itemId)) continue;
            section.ConfirmedItemIds.Add(itemId);
            changed = true;
        }

        if (changed) MarkDirty();
    }

    /// <summary>
    ///     Plays a single offered follow-up the server may speak itself (NPC, System, PlayerAuto lines) and stops at
    ///     the first item the client has to confirm: a player reply or a mission card. Replaying every official
    ///     section transition with this stop reproduces the official offers and histories; walking through the
    ///     replies as well answered the player's own lines server-side (four picks of section 1211700 in one
    ///     instant). Played lines stay out of the history, which only lists client picks.
    /// </summary>
    private static void AutoAdvance(MessageSectionData section)
    {
        var visited = new HashSet<int>();
        while (section.ToChooseItemId.Count == 1)
        {
            var nextId = section.ToChooseItemId[0];
            if (!GameData.MessageItemConfigData.TryGetValue(nextId, out var next) || next.WaitsForClient()) return;
            // The visited set stops a config chain that loops.
            if (!visited.Add(nextId))
            {
                section.ToChooseItemId = [];
                return;
            }

            section.ToChooseItemId = [.. next.NextItemIDList];
        }
    }

    #endregion
}
