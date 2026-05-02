using March7thHoney.GameServer.Game.GridFight.Component;
using March7thHoney.Proto;
using March7thHoney.Proto.ServerSide;

namespace March7thHoney.GameServer.Game.GridFight.Sync;

public class GridFightRoleUpdateSyncData(GridFightSrc src, GridFightRoleInfoPb role, uint groupId = 0, params uint[] param) : BaseGridFightSyncData(src, groupId, param)
{
    public GridFightRoleInfoPb Role { get; set; } = role;

    public override GridFightSyncData ToProto()
    {
        return new GridFightSyncData
        {
            UpdateRoleInfo = Role.ToProto()
        };
    }
}