using March7thHoney.GameServer.Game.GridFight.Component;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.GridFight.Sync;

public class GridFightRoleDamageSttSyncData(GridFightSrc src, GridFightLevelComponent comp, uint groupId = 0, params uint[] param) : BaseGridFightSyncData(src, groupId, param)
{
    public override GridFightSyncData ToProto()
    {
        return new GridFightSyncData
        {
            GridFightDamageSttInfo = comp.ToDamageSttInfo()
        };
    }
}