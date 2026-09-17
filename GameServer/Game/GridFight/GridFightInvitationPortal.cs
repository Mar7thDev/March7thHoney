namespace March7thHoney.GameServer.Game.GridFight;

/// <summary>
/// Official 4.5 invitation portals (1101-1119): selecting the portal grants one fixed emblem
/// before the normal initial supply is materialized. Portal 1118 was packet-captured directly;
/// the remaining portal->emblem mapping follows the invitation names and the server tool's
/// GridFight equipment ID table.
/// </summary>
public sealed partial class GridFightSession
{
    public const uint InvitationEquipmentSource = 1;
    public const uint InvitationPortalEffectId = 102;

    private static readonly IReadOnlyDictionary<uint, uint> InvitationPortalEquipmentMap =
        new Dictionary<uint, uint>
        {
            [1101] = 35051001, // 列车同行邀请 -> 列车同行星徽
            [1102] = 35051003, // 银河学者邀请 -> 银河学者星徽
            [1103] = 35051005, // 贝洛伯格邀请 -> 贝洛伯格星徽
            [1104] = 35051006, // 星间旅人邀请 -> 星间旅人星徽
            [1105] = 35051007, // 仙舟邀请 -> 仙舟星徽
            [1106] = 35051008, // 狼狩邀请 -> 狼狩星徽
            [1107] = 35051010, // 盛会之星邀请 -> 盛会之星星徽
            [1108] = 35051011, // 昼之半神邀请 -> 昼之半神星徽
            [1109] = 35051012, // 夜之半神邀请 -> 夜之半神星徽
            [1110] = 35052001, // 追击邀请 -> 追击星徽
            [1111] = 35052002, // 击破邀请 -> 击破星徽
            [1112] = 35052003, // 群攻邀请 -> 群攻星徽
            [1113] = 35052004, // 能量邀请 -> 能量星徽
            [1114] = 35052006, // 燃血邀请 -> 燃血星徽
            [1115] = 35052007, // 减益邀请 -> 减益星徽
            [1116] = 35052008, // 持续伤害邀请 -> 持续伤害星徽
            [1117] = 35052009, // 量子同频邀请 -> 量子同频星徽
            [1118] = 35052011, // 战技点邀请 -> 战技点星徽（官服抓包已验证）
            [1119] = 35052012, // 欢愉邀请 -> 欢愉星徽
        };

    public GridFightEquipmentState? InvitationPortalEquipment { get; private set; }

    public bool HasInvitationPortal => InvitationPortalEquipmentMap.ContainsKey(SelectedPortalId);

    private void InitializeInvitationPortal()
    {
        InvitationPortalEquipment = null;
        if (!InvitationPortalEquipmentMap.TryGetValue(SelectedPortalId, out var equipmentId)) return;

        // Official 1118 packet creates the emblem before initial roles, source=1, and leaves it
        // as a normal unlocked inventory equipment instance.
        if (TryAddEquipment(equipmentId, out var equipment, InvitationEquipmentSource))
            InvitationPortalEquipment = equipment;
    }
}
