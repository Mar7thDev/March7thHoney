using March7thHoney.GameServer.Game.Battle;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server.Packet.Send.GridFight;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.GridFight;

public class GridFightInstance(PlayerInstance player, uint season, uint divisionId, bool isOverLock, uint uniqueId)
{
    public PlayerInstance Player { get; } = player;
    public uint Season { get; } = season;
    public uint DivisionId { get; } = divisionId;
    public bool IsOverLock { get; } = isOverLock;
    public uint UniqueId { get; } = uniqueId;
    public uint QueuePosition { get; set; } = 1;
    public List<uint> StageCandidates { get; } = [35030205, 35030405, 35030208, 350202, 35030606];
    public Dictionary<uint, uint> RoleByUniqueId { get; } = new();
    public Dictionary<uint, uint> UniqueIdByPos { get; } = new();

    public void EnsureDefaultRoles()
    {
        if (RoleByUniqueId.Count > 0)
            return;

        var defaultAvatarIds = new uint[] { 1223, 1201, 1006, 1301, 1202, 15062, 15063, 1401, 1306 };
        for (var i = 0; i < defaultAvatarIds.Length; i++)
            RoleByUniqueId[(uint)(i + 1)] = defaultAvatarIds[i];
    }

    public void ApplyPositionList(IEnumerable<GridFightPosInfo> posInfoList)
    {
        EnsureDefaultRoles();
        foreach (var posInfo in posInfoList)
        {
            if (posInfo.Pos == 0)
                continue;

            UniqueIdByPos[posInfo.Pos] = posInfo.UniqueId;
            if (posInfo.UniqueId != 0 && !RoleByUniqueId.ContainsKey(posInfo.UniqueId))
                RoleByUniqueId[posInfo.UniqueId] = 8001;
        }
    }

    public List<int> BuildBattleAvatarIds(int maxCount = 4)
    {
        EnsureDefaultRoles();

        var list = new List<int>();
        foreach (var (pos, uniqueId) in UniqueIdByPos.OrderBy(x => x.Key))
        {
            _ = pos;
            if (uniqueId == 0 || !RoleByUniqueId.TryGetValue(uniqueId, out var avatarId))
                continue;
            list.Add((int)avatarId);
            if (list.Count >= maxCount)
                break;
        }

        return list;
    }

    public GridFightCurrentInfo ToProto()
    {
        return new GridFightCurrentInfo
        {
            Season = Season,
            DivisionId = DivisionId,
            IsOverlock = IsOverLock,
            UniqueId = UniqueId,
            PendingAction = new GridFightPendingAction
            {
                QueuePosition = QueuePosition,
                PortalBuffAction = new GridFightPortalBuffActionInfo
                {
                    FCHPJKAIBHB = 1,
                    GridFightPortalBuffList = { 1115u, 106u, 147u }
                }
            },
            BCHPAOCOHIL = new FCBEHGJBJCN
            {
                AMNJHJJMPJF = { StageCandidates }
            },
            GridFightGameData = new GridFightGameData()
        };
    }

    public BattleInstance? StartBattle()
    {
        return Player.BattleManager!.StartGridFightBattle(this);
    }

    public async ValueTask EndBattle(BattleInstance battle, PVEBattleResultCsReq req)
    {
        await Player.SendPacket(new PacketGridFightEndBattleStageNotify());
        await Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(Player));
    }
}
