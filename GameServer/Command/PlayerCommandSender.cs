using March7thHoney.Command;
using March7thHoney.Database;
using March7thHoney.Database.Account;
using March7thHoney.GameServer.Game.Player;
// TODO 4.3: Chat 模块暂停
using March7thHoney.Util;

namespace March7thHoney.GameServer.Command;

public class PlayerCommandSender(PlayerInstance player) : ICommandSender
{
    public PlayerInstance Player = player;

    public async ValueTask SendMsg(string msg)
    {
        var sendTime = Extensions.GetUnixSec();
        // TODO 4.3: PacketRevcMsgScNotify (Chat 模块) 暂停
        _ = sendTime;
        _ = msg;
    }

    public bool HasPermission(string permission)
    {
        var account = DatabaseHelper.Instance!.GetInstance<AccountData>(Player.Uid)!;
        return account.HasPermission(permission);
    }

    public int GetSender()
    {
        return Player.Uid;
    }
}
