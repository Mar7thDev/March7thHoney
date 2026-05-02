using March7thHoney.Proto;
using March7thHoney.Proto.ServerSide;

namespace March7thHoney.GameServer.Game.GridFight.Sync;

public class GridFightPlayerLevelSyncData(GridFightSrc src, GridFightBasicInfoPb info, uint groupId = 0, params uint[] param) : BaseGridFightSyncData(src, groupId, param)
{
    public override GridFightSyncData ToProto()
    {
        return new GridFightSyncData
        {
            PlayerLevel = new GridFightPlayerLevelSyncInfo
            {
                Exp = info.LevelExp,
                Level = info.CurLevel,
                MaxLevel = info.MaxLevel
            }
        };
    }
}