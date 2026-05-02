using March7thHoney.Proto;
using March7thHoney.Proto.ServerSide;

namespace March7thHoney.GameServer.Game.GridFight.Sync;

public class GridFightMaxAvatarNumSyncData(GridFightSrc src, GridFightBasicInfoPb info, uint groupId = 0, params uint[] param) : BaseGridFightSyncData(src, groupId, param)
{
    public override GridFightSyncData ToProto()
    {
        return new GridFightSyncData
        {
            MaxBattleRoleNum = Math.Min(13, info.MaxAvatarNum)
        };
    }
}