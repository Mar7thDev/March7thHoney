using March7thHoney.Database.Account;
using March7thHoney.GameServer.Server.Packet.Send.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Server;

public static class ConnectionDisconnectHelper
{
    private static readonly Logger Logger = new("GameServer");
    private static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(1);

    public static void KickByGm(Connection? connection)
    {
        Disconnect(connection, new PacketPlayerSqueezedScNotify(KickType.KickByGm));
    }

    public static void KickBanned(Connection? connection, AccountBanStatus banStatus)
    {
        Disconnect(connection, new PacketPlayerSqueezedScNotify(KickType.KickBlack,
            AccountBanHelper.ToBlackInfo(banStatus)));
    }

    private static void Disconnect(Connection? connection, BasePacket packet)
    {
        if (connection == null)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                var sendTask = connection.SendPacket(packet);
                var completedTask = await Task.WhenAny(sendTask, Task.Delay(SendTimeout));
                if (completedTask == sendTask)
                    await sendTask;
                else
                    Logger.Warn($"Timed out sending disconnect packet to {connection.RemoteEndPoint}.");
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to send disconnect packet to {connection.RemoteEndPoint}.", ex);
            }
            finally
            {
                try
                {
                    connection.Stop();
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Failed to stop connection {connection.RemoteEndPoint}.", ex);
                }
            }
        });
    }
}
