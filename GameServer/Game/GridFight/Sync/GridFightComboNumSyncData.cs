using March7thHoney.Proto;
using March7thHoney.Proto.ServerSide;

namespace March7thHoney.GameServer.Game.GridFight.Sync;

public class GridFightComboNumSyncData(GridFightSrc src, GridFightBasicInfoPb info, params uint[] param) : BaseGridFightSyncData(src, 0, param)
{
    public override GridFightSyncData ToProto()
    {
        return new GridFightSyncData
        {
            GridFightComboWinNum = info.ComboNum
        };
    }
}