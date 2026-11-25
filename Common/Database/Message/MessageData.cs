using MemoryPack;
using March7thHoney.Proto;

namespace March7thHoney.Database.Message;

[DbTable("Message")]
public class MessageData : BaseDatabaseDataHelper
{
    public Dictionary<int, MessageGroupData> Groups { get; set; } = [];
}

[MemoryPackable]
public partial class MessageGroupData
{
    public int GroupId { get; set; }
    public List<MessageSectionData> Sections { get; set; } = [];
    public MessageGroupStatus Status { get; set; } = MessageGroupStatus.MessageGroupNone;
    public long RefreshTime { get; set; }
    public int CurrentSectionId { get; set; }
}

[MemoryPackable]
public partial class MessageSectionData
{
    public int SectionId { get; set; }
    public MessageSectionStatus Status { get; set; } = MessageSectionStatus.MessageSectionNone;
    public List<MessageItemData> Items { get; set; } = [];
    public List<int> ToChooseItemId { get; set; } = [];

    // Append-only below (MemoryPack member order). The items the client confirmed, in confirmation order, plus a
    // mission card the section retired when it finished: the history the client renders. Lines the server played on
    // its own stay out of it. A save written before this member existed reads it back as null.
    public List<int> ConfirmedItemIds { get; set; } = [];

    [MemoryPackOnDeserialized]
    private void FillMissingMembers()
    {
        ConfirmedItemIds ??= [];
    }
}

[MemoryPackable]
public partial class MessageItemData
{
    public int ItemId { get; set; }
}
