using CastoriceHoney.Server.Command;
using CastoriceHoney.Server.Database;
using CastoriceHoney.Server.Database.Account;
using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.GameServer.Server.Packet.Send.Chat;
using CastoriceHoney.Server.Util;

namespace CastoriceHoney.Server.GameServer.Command;

public class PlayerCommandSender(PlayerInstance player) : ICommandSender
{
    public PlayerInstance Player = player;

    public async ValueTask SendMsg(string msg)
    {
        await Player.SendPacket(new PacketRevcMsgScNotify((uint)Player.Uid,
            (uint)ConfigManager.Config.ServerOption.ServerProfile.Uid, msg.Replace("\n", "    ")));
    }

    public bool HasPermission(string permission)
    {
        var account = DatabaseHelper.Instance!.GetInstance<AccountData>(Player.Uid)!;
        return account.Permissions!.Contains(permission);
    }

    public int GetSender()
    {
        return Player.Uid;
    }
}