using System.Net;
using System.Net.Sockets;
using March7thHoney.Internationalization;
using March7thHoney.Util;

namespace March7thHoney.Kcp;

// TODO 4.3: 4.3 客户端把 GameServer 传输从 KCP/UDP 切到 TCP。
// 类名/命名空间保持 March7thHoney.Kcp.* 以减少上层改动，但底层是 TcpListener。
public class March7thHoneyListener
{
    public delegate March7thHoneyConnection ConnectionCreatedHandler(TcpClient client, IPEndPoint remote, long conversationId);

    private static TcpListener? TcpListener;
    private static readonly Logger Logger = new("GameServer");
    public static readonly SortedList<long, March7thHoneyConnection> Connections = [];
    private static long _nextConversationId;

    public static ConnectionCreatedHandler? CreateConnection { get; set; }

    private static uint PORT => ConfigManager.Config.GameServer.Port;

    public static March7thHoneyConnection? GetConnectionByEndPoint(IPEndPoint ep)
    {
        return Connections.Values.FirstOrDefault(c => c.RemoteEndPoint.Equals(ep));
    }

    public static void StartListener()
    {
        var bind = IPAddress.Parse(ConfigManager.Config.GameServer.BindAddress);
        TcpListener = new TcpListener(bind, (int)PORT);
        TcpListener.Start();
        Logger.Info(I18NManager.Translate("Server.ServerInfo.ServerRunning", I18NManager.Translate("Word.Game"),
            ConfigManager.Config.GameServer.GetDisplayAddress()));
        _ = Task.Run(AcceptLoopAsync);
    }

    private static async Task AcceptLoopAsync()
    {
        while (TcpListener != null)
        {
            TcpClient client;
            try
            {
                client = await TcpListener.AcceptTcpClientAsync();
            }
            catch (Exception ex)
            {
                Logger.Error("Accept loop crashed", ex);
                break;
            }

            var remote = (IPEndPoint?)client.Client.RemoteEndPoint ?? new IPEndPoint(IPAddress.None, 0);
            var convId = Interlocked.Increment(ref _nextConversationId);
            Logger.Info($"Accepting TCP connection from {remote}. conv_id={convId}");

            var handler = CreateConnection;
            if (handler == null)
            {
                client.Close();
                continue;
            }

            var con = handler(client, remote, convId);
            RegisterConnection(con);
        }
    }

    private static void RegisterConnection(March7thHoneyConnection con)
    {
        if (!con.ConversationId.HasValue) return;
        lock (Connections)
            Connections[con.ConversationId.Value] = con;
    }

    public static void UnregisterConnection(March7thHoneyConnection con)
    {
        if (!con.ConversationId.HasValue) return;
        lock (Connections)
        {
            if (Connections.Remove(con.ConversationId.Value))
                Logger.Info($"Connection with {con.RemoteEndPoint} has been closed");
        }
    }
}
