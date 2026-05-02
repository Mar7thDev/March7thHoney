using CastoriceHoney.Server.Proto;
using CastoriceHoney.Server.Proto.ServerSide;

namespace CastoriceHoney.Server.GameServer.Game.GridFight.Sync;

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