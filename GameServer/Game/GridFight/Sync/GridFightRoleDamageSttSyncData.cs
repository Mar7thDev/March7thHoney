using CastoriceHoney.Server.GameServer.Game.GridFight.Component;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Game.GridFight.Sync;

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