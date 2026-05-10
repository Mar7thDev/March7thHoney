using March7thHoney.GameServer.Game.GridFight;
using March7thHoney.GameServer.Game.Battle;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightEnterBattleStageScRsp : BasePacket
{
    public PacketGridFightEnterBattleStageScRsp(GridFightInstance? inst, BattleInstance? battle) : base(CmdIds.GridFightEnterBattleStageScRsp)
    {
        var proto = new GridFightEnterBattleStageScRsp();
        if (inst == null || battle == null)
            proto.Retcode = (uint)Retcode.RetFail;
        else
            proto.BattleInfo = BuildBattleInfo(inst, battle);
        SetData(proto);
    }

    private static SceneBattleInfo BuildBattleInfo(GridFightInstance inst, BattleInstance battle)
    {
        var info = battle.ToProto();

        info.AFCMOOFGBPK = new OGEOMDJIAGI
        {
            BBDOCJGAEEJ = inst.DivisionId,
            LFKBMDHKPFI = (uint)battle.StageId,
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
        };

        foreach (var portalBuffId in inst.ActivePortalBuffIds)
            info.AFCMOOFGBPK.GridFightPortalBuffList.Add(new MMDJJDEJMMN { PortalBuffId = portalBuffId });
        info.AFCMOOFGBPK.OGHGLMGJGEM["GP_Avatar_Sparxie_00"] = 2;

        info.AFCMOOFGBPK.PIDIGFGKAMK.Add(info.BattleAvatarList);

        foreach (var (pos, uniqueId) in inst.UniqueIdByPos.OrderBy(kv => kv.Key))
        {
            if (pos == 0 || pos > 13) continue;
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

        return info;
    }
}
