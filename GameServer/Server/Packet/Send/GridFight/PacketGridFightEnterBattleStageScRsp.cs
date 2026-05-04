using March7thHoney.GameServer.Game.GridFight;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightEnterBattleStageScRsp : BasePacket
{
    public PacketGridFightEnterBattleStageScRsp(GridFightInstance? inst) : base(CmdIds.GridFightEnterBattleStageScRsp)
    {
        var proto = new GridFightEnterBattleStageScRsp();
        if (inst == null)
            proto.Retcode = (uint)Retcode.RetFail;
        else
            proto.BattleInfo = BuildBattleInfo(inst);
        SetData(proto);
    }

    // 抓包 seq 462: SceneBattleInfo 顶层有 stageId/battleId/battleAvatarList/monsterWaveList 等标准字段，
    // 同时带 AFCMOOFGBPK（OGEOMDJIAGI）子字段承载货币战争专属数据
    private static SceneBattleInfo BuildBattleInfo(GridFightInstance inst)
    {
        var avatars = BuildBattleAvatars(inst);

        var info = new SceneBattleInfo
        {
            BattleId = 1,
            StageId = 90303,
            WorldLevel = 6,
            LogicRandomSeed = (uint)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() & 0x7fffffff),
            AFCMOOFGBPK = new OGEOMDJIAGI
            {
                BBDOCJGAEEJ = inst.DivisionId,
                LFKBMDHKPFI = 90303,
                ANBBPPHBCJH = 3,
                HDCAEIKAPND = 3,
                OOPPKDAFFDG = 44,
                Season = inst.Season,
                IsOverlock = inst.IsOverLock,
                GridFightLineupHp = inst.LineupHp,
                OIHHKOJFHFG = new OMBNHGAJANJ
                {
                    BNLHIMHFGDK = 1,
                    DCPKPNLKGMM = inst.Level,
                    NDOCIKPLKIF = 1600,
                    SectionId = inst.SectionId
                }
            }
        };
        foreach (var portalBuffId in inst.ActivePortalBuffIds)
            info.AFCMOOFGBPK.GridFightPortalBuffList.Add(new MMDJJDEJMMN { PortalBuffId = portalBuffId });
        info.AFCMOOFGBPK.OGHGLMGJGEM["GP_Avatar_Sparxie_00"] = 2;

        foreach (var av in avatars)
        {
            info.BattleAvatarList.Add(av);
            info.AFCMOOFGBPK.PIDIGFGKAMK.Add(av);
        }

        foreach (var (pos, uniqueId) in inst.UniqueIdByPos.OrderBy(kv => kv.Key))
        {
            if (pos == 0 || pos > 4) continue;
            if (uniqueId == 0 || !inst.RoleByUniqueId.TryGetValue(uniqueId, out var avatarId)) continue;

            var roleInfo = new JAJOBJJPINN
            {
                RoleId = avatarId,
                AvatarId = avatarId,
                Pos = pos,
                UniqueId = uniqueId,
                RoleStar = inst.RoleStarByUniqueId.GetValueOrDefault(uniqueId, 1u),
                GJEHIGGNIAP = new IFDFHPAMHCL()
            };
            roleInfo.ConvertPropertyToFixpoint.Add(32, 180);
            roleInfo.ConvertPropertyToFixpoint.Add(33, 480);
            roleInfo.ConvertPropertyToFixpoint.Add(34, 440);
            roleInfo.ConvertPropertyToFixpoint.Add(44, 304);
            roleInfo.ConvertPropertyToFixpoint.Add(49, 180);
            roleInfo.ConvertPropertyToFixpoint.Add(52, 240);
            roleInfo.ConvertPropertyToFixpoint.Add(53, 480);
            roleInfo.ConvertPropertyToFixpoint.Add(57, 240);
            roleInfo.ConvertPropertyToFixpoint.Add(59, 450);
            roleInfo.ConvertPropertyToFixpoint.Add(1013, 100);
            roleInfo.ConvertPropertyToFixpoint.Add(1014, 100);
            roleInfo.ConvertPropertyToFixpoint.Add(1019, 100);
            roleInfo.ConvertPropertyToFixpoint.Add(1025, 100);
            info.AFCMOOFGBPK.GridGameRoleList.Add(roleInfo);
        }

        // 怪物波（参考抓包 seq 462 一波 3 怪）
        info.MonsterWaveList.Add(new SceneMonsterWave
        {
            BattleStageId = 90303,
            BattleWaveId = 1,
            MonsterParam = new SceneMonsterWaveParam(),
            MonsterList =
            {
                new SceneMonster { MonsterId = 300101007 },
                new SceneMonster { MonsterId = 800102021 },
                new SceneMonster { MonsterId = 300101007 }
            }
        });

        return info;
    }

    private static List<BattleAvatar> BuildBattleAvatars(GridFightInstance inst)
    {
        var list = new List<BattleAvatar>();
        uint index = 0;
        foreach (var (pos, uniqueId) in inst.UniqueIdByPos.OrderBy(kv => kv.Key))
        {
            if (uniqueId == 0 || !inst.RoleByUniqueId.TryGetValue(uniqueId, out var avatarId)) continue;
            if (pos > 4) continue; // 仅前 4 位为出战阵容
            list.Add(BuildAvatar(avatarId, index++));
            if (list.Count >= 4) break;
        }
        // 兜底：若前 4 位没角色，从所有角色取最多 4 个
        if (list.Count == 0)
        {
            foreach (var (uniqueId, avatarId) in inst.RoleByUniqueId)
            {
                _ = uniqueId;
                list.Add(BuildAvatar(avatarId, index++));
                if (list.Count >= 4) break;
            }
        }
        return list;
    }

    private static BattleAvatar BuildAvatar(uint avatarId, uint index) => new()
    {
        AvatarType = AvatarType.AvatarGridFightType,
        Id = avatarId,
        Level = 80,
        Promotion = 6,
        Rank = 6,
        Index = index,
        Hp = 10000,
        WorldLevel = 6,
        SpBar = new SpBarInfo { CurSp = 10000, MaxSp = 10000 },
        PJLADGOOFKE = 4772
    };
}
