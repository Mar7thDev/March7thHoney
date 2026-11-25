using MemoryPack;
using Newtonsoft.Json;

namespace March7thHoney.Data.Excel;

[ResourceEntity("MessageItemConfig.json")]
[MemoryPackable]
public partial class MessageItemConfigExcel : ExcelResource
{
    public int ID { get; set; }
    public List<int> NextItemIDList { get; set; } = [];
    public int SectionID { get; set; }

    [JsonIgnore] public int GroupID { get; set; }

    // Append-only below (MemoryPack member order). Who speaks the bubble (NPC, Player, PlayerAuto, System) and what
    // it is (Text, Sticker, Raid, Link, ...): the message manager needs both to tell a reply the player must pick
    // and a mission card from a line the server may play on its own. A cache built before they existed has none.
    public string Sender { get; set; } = "";
    public string ItemType { get; set; } = "";

    [MemoryPackOnDeserialized]
    private void FillMissingMembers()
    {
        Sender ??= "";
        ItemType ??= "";
    }

    /// <summary>
    ///     A mission card (ItemType Raid or Link), such as "Unlock Dan Heng's story (I)". Official captures keep it
    ///     offered while the section runs, which is what pins the chat and shows the mission icon; no client
    ///     request ever picks one.
    /// </summary>
    public bool IsMissionCard()
    {
        return ItemType is "Raid" or "Link";
    }

    /// <summary>
    ///     The item stays offered until the client confirms it: a reply the player picks (Sender Player) or a
    ///     mission card. Every other single follow-up (NPC, System, PlayerAuto) is played by the server.
    /// </summary>
    public bool WaitsForClient()
    {
        return Sender == "Player" || IsMissionCard();
    }

    public override int GetId()
    {
        return ID;
    }

    public override void Loaded()
    {
        GameData.MessageItemConfigData.Add(ID, this);
    }

    public override void AfterAllDone()
    {
        if (GameData.MessageSectionConfigData.TryGetValue(SectionID, out var value))
        {
            value.Items.Add(this);
            GroupID = value.GroupID;
        }
    }
}
