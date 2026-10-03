using March7thHoney.Data;
using March7thHoney.Database;
using March7thHoney.Database.Pet;
using March7thHoney.Enums.Item;
using March7thHoney.GameServer.Game.Player.Components;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.Pet;

/// <summary>
///     4.6 盲盒宠物（移植自 yuukisr_go c4f5979 / 24e7083）。
///     <para>
///         宠物物品仍作为材料堆叠在背包里（/giveall、奖励等发放路径不变），但官服从不把宠物物品放进背包同步，
///         客户端的宠物页完全由 <c>GetPetDataScRsp</c> 的实例列表构建。所以这里按材料数量对账出实例：多出来的
///         份数追加实例，少掉的份数删掉实例并清理编队引用。实例 id 持久化在 <see cref="PlayerPetData" />。
///     </para>
///     <para>
///         只有盲盒宠物（4.6 新增，251501 起）会成为实例；一代召唤宠物（PetConfig 的 251001–251005，
///         ItemSubType NormalPet）进 pet_list 会让客户端 BlindBoxPetRepository 抛 ArgumentException，
///         整个宠物模块初始化失败。
///     </para>
/// </summary>
public class PetManager(PlayerInstance player) : BasePlayerComponent(player)
{
    /// <summary>官服抓包编队带 slot 1..3，BlindBoxPetType 里在用的类型也是 3 槽。BlindBox* 表未进缓存，先写死。</summary>
    public const int MaxBlindBoxLineupSlots = 3;

    // 官服枚举值（yuukisr_go design_pet_system.md §4.5）：有宠物的账号 cur_pet.ref_type=3、编队 source=2，新号都是 0。
    public const int CurPetRefTypeBlindBox = (int)EOGNOEJHAGN.Lcfkgcjchbo;
    public const HENPKNCKGGI LineupSourceSet = HENPKNCKGGI.Oalpdgkgpbj;

    // 宠物 unique id 取高位，和官服的大数 id 同一量级，同时保证不会和其他小整数 id 混淆。
    private const ulong UniqueIdBase = 1UL << 62;

    public PlayerPetData Data { get; } = DatabaseHelper.Instance!.GetInstanceOrCreateNew<PlayerPetData>(player.Uid);

    private void MarkDirty() => DatabaseHelper.MarkDirty(Player.Uid);

    /// <summary>
    ///     盲盒宠物物品。缓存里的 ItemSubType 枚举没有 BlindBoxPet（解析为 Unknown），所以按排除法判断：
    ///     主类型为 Pet，且既不是 NormalPet 子类型，也不是 PetConfig 里一代宠物的物品。
    /// </summary>
    public static bool IsBlindBoxPetItem(int itemId)
    {
        if (!GameData.ItemConfigData.TryGetValue(itemId, out var config)) return false;
        if (config.ItemMainType != ItemMainTypeEnum.Pet) return false;
        if (config.ItemSubType == ItemSubTypeEnum.NormalPet) return false;
        return GameData.PetData.Values.All(pet => pet.PetItemID != itemId);
    }

    /// <summary>
    ///     按背包里的盲盒宠物材料对账实例与编队，返回本次新建的实例。已有实例保持原有顺序与 id。
    /// </summary>
    public List<PetInstanceData> Reconcile()
    {
        var owned = new Dictionary<int, int>();
        var order = new List<int>();
        foreach (var item in Player.InventoryManager!.Data.MaterialItems)
        {
            if (item.Count <= 0 || !IsBlindBoxPetItem(item.ItemId)) continue;
            if (!owned.ContainsKey(item.ItemId)) order.Add(item.ItemId);
            owned[item.ItemId] = owned.GetValueOrDefault(item.ItemId) + item.Count;
        }

        var kept = new Dictionary<int, int>();
        var instances = new List<PetInstanceData>(Data.Instances.Count);
        foreach (var instance in Data.Instances)
        {
            if (kept.GetValueOrDefault(instance.PetId) >= owned.GetValueOrDefault(instance.PetId)) continue;
            kept[instance.PetId] = kept.GetValueOrDefault(instance.PetId) + 1;
            instances.Add(instance);
        }

        var changed = instances.Count != Data.Instances.Count;
        var added = new List<PetInstanceData>();
        foreach (var petId in order)
            for (var have = kept.GetValueOrDefault(petId); have < owned[petId]; have++)
            {
                Data.NextSerial++;
                var instance = new PetInstanceData { UniqueId = UniqueIdBase | Data.NextSerial, PetId = petId };
                instances.Add(instance);
                added.Add(instance);
                kept[petId] = have + 1;
                changed = true;
            }

        Data.Instances = instances;

        var exists = instances.Select(x => x.UniqueId).ToHashSet();
        var lineup = Data.Lineup.Where(slot => exists.Remove(slot.UniqueId)).ToList();
        if (lineup.Count != Data.Lineup.Count)
        {
            Data.Lineup = lineup;
            changed = true;
        }

        if (changed) MarkDirty();
        return added;
    }

    /// <summary>
    ///     背包里的宠物材料变化后调用：对账，并按官服抽盒流程给每只新宠物推一条 PetDataChangeScNotify{add_pet}，
    ///     客户端据此把新实例加入宠物页。删除方向官服没有抓包证据，不推送，下一次 GetPetData 自然同步。
    /// </summary>
    public async ValueTask OnPetItemsChanged()
    {
        var added = Reconcile();
        if (added.Count == 0) return;

        var notify = new PetDataChangeScNotify();
        foreach (var instance in added)
            notify.DGNGMJMPFGK.Add(new CBHJJCBDJHA { AHBNBNFABBO = ToInstanceProto(instance) });
        await Player.SendPacket(new Server.Packet.Send.Pet.PacketPetDataChangeScNotify(notify));
    }

    /// <summary>
    ///     替换盲盒宠物编队。实例必须已拥有且不重复，槽位不超过 3 个、槽号不重复且不大于 3；不合法时不做任何修改。
    /// </summary>
    public bool SetBlindBoxLineup(IEnumerable<EPFJLNKPEPK> slots)
    {
        var requested = slots.ToList();
        if (requested.Count > MaxBlindBoxLineupSlots) return false;

        Reconcile();
        var owned = Data.Instances.Select(x => x.UniqueId).ToHashSet();
        var seenIds = new HashSet<ulong>();
        var seenSlots = new HashSet<uint>();
        foreach (var slot in requested)
        {
            if (slot.EBAAFDHEPLO == 0 || !owned.Contains(slot.EBAAFDHEPLO)) return false;
            if (!seenIds.Add(slot.EBAAFDHEPLO)) return false;
            if (slot.Slot > MaxBlindBoxLineupSlots || !seenSlots.Add(slot.Slot)) return false;
        }

        Data.Lineup = [.. requested.Select(x => new PetLineupSlotData { UniqueId = x.EBAAFDHEPLO, Slot = (int)x.Slot })];
        MarkDirty();
        return true;
    }

    /// <summary>召唤：记录请求的召唤方式，一代宠物 id 同时写回旧的 PlayerData.Pet。返回回包用的 cur_pet。</summary>
    public EHDNPMEKPCA Summon(SummonPetCsReq req)
    {
        Data.CurPetRefType = (int)req.LLGIIPALELD;
        Player.Data.Pet = req.HGEOKHHJAIECase == SummonPetCsReq.HGEOKHHJAIEOneofCase.SummonedPetId
            ? (int)req.SummonedPetId
            : 0;
        MarkDirty();

        var cur = new EHDNPMEKPCA { LLGIIPALELD = req.LLGIIPALELD };
        switch (req.HGEOKHHJAIECase)
        {
            case SummonPetCsReq.HGEOKHHJAIEOneofCase.SummonedPetId:
                cur.ODNGMIJLKKA = req.SummonedPetId;
                break;
            case SummonPetCsReq.HGEOKHHJAIEOneofCase.EBAAFDHEPLO:
                cur.EBAAFDHEPLO = req.EBAAFDHEPLO;
                break;
        }

        return cur;
    }

    /// <summary>收回：官服回包带一个空的 cur_pet。</summary>
    public void Recall()
    {
        Data.CurPetRefType = 0;
        Player.Data.Pet = 0;
        MarkDirty();
    }

    public GetPetDataScRsp ToGetPetDataProto()
    {
        Reconcile();
        var proto = new GetPetDataScRsp
        {
            // 两个子消息官服总是带上，即使为空（新号抓包）。
            PLKBLNMOOPH = new EHDNPMEKPCA
            {
                LLGIIPALELD = (EOGNOEJHAGN)(Data.CurPetRefType >= 0
                    ? Data.CurPetRefType
                    : Data.Instances.Count > 0
                        ? CurPetRefTypeBlindBox
                        : 0)
            },
            // 编队来源：官服有编队时为 2，没有时为 0。
            LCFKGCJCHBO = ToLineupProto(Data.Lineup.Count > 0 ? LineupSourceSet : HENPKNCKGGI.Cdcciofmcgn)
        };
        // unlocked_pet_id 是一代宠物的旧字段，4.6 有宠物的官服账号抓包里不带，这里也不发。
        proto.MPJNPINHCJL.AddRange(Data.Instances.Select(ToInstanceProto));
        return proto;
    }

    /// <summary>设置编队的回包与通知原样回显请求里的来源枚举（官服请求不带，回显也不带）。</summary>
    public CMBGLKEMOGF ToLineupProto(HENPKNCKGGI sourceType)
    {
        var proto = new CMBGLKEMOGF { CGBJOAJEMKN = sourceType };
        proto.POJGKJENENA.AddRange(Data.Lineup.Select(slot => new EPFJLNKPEPK
        {
            EBAAFDHEPLO = slot.UniqueId,
            Slot = (uint)slot.Slot
        }));
        return proto;
    }

    public static JGDOHOFOFOF ToInstanceProto(PetInstanceData instance)
    {
        return new JGDOHOFOFOF
        {
            EBAAFDHEPLO = instance.UniqueId,
            PetId = (uint)instance.PetId,
            HEOHMOFNOIH = (uint)instance.ChromaLevel,
            IEBIKNPNPNO = instance.ChromaActive
        };
    }
}
