using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Game.Sync.Player;

public class PlayerBoardSync(PlayerInstance player) : BaseSyncData
{
    public override void SyncData(in PlayerSyncScNotify notify)
    {
        notify.PlayerboardModuleSync = new PlayerBoardModuleSync
        {
            Signature = player.Data.Signature,
            HeadFrameInfo = player.Data.HeadFrame.ToProto()
        };
    }
}
