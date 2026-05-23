using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using March7thHoney.Util;

namespace March7thHoney.Kcp;

// TODO 4.3: 由 KCP/UDP 迁到 TCP。保留 March7thHoneyConnection 类名 + 主要接口
// （ConversationId / SendPacket / Stop 等）以减少上层改动。
public class March7thHoneyConnection
{
    public static readonly ConcurrentBag<int> BannedPackets = [];
    private static readonly Logger Logger = new("GameServer");
    public static readonly ConcurrentDictionary<int, string> LogMap = [];

    public static readonly ConcurrentBag<int> IgnoreLog =
    [
        CmdIds.PlayerHeartBeatCsReq, CmdIds.PlayerHeartBeatScRsp, CmdIds.SceneEntityMoveCsReq,
        CmdIds.SceneEntityMoveScRsp, CmdIds.GetShopListCsReq, CmdIds.GetShopListScRsp
    ];

    protected readonly CancellationTokenSource CancelToken;
    protected readonly TcpClient TcpClient;
    protected readonly NetworkStream Stream;
    private readonly long _conversationId;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    public readonly IPEndPoint RemoteEndPoint;

    public string DebugFile = "";
    public bool IsOnline = true;
    public StreamWriter? Writer;

    public March7thHoneyConnection(TcpClient client, IPEndPoint remote, long conversationId)
    {
        TcpClient = client;
        Stream = client.GetStream();
        RemoteEndPoint = remote;
        _conversationId = conversationId;
        CancelToken = new CancellationTokenSource();
        if (ConfigManager.Config.GameServer.UsePacketEncryption) XorKey = Crypto.ClientSecretKey!.GetXorKey();

        Start();
    }

    public byte[]? XorKey { get; set; }
    public ulong ClientSecretKeySeed { get; set; }

    public long? ConversationId => _conversationId;

    public SessionStateEnum State { get; set; } = SessionStateEnum.INACTIVE;

    public virtual void Start()
    {
        Logger.Info($"New connection from {RemoteEndPoint}.");
        State = SessionStateEnum.WAITING_FOR_TOKEN;
    }

    public virtual void Stop()
    {
        try
        {
            CancelToken.Cancel();
        }
        catch
        {
        }

        try
        {
            Stream.Close();
        }
        catch
        {
        }

        try
        {
            TcpClient.Close();
        }
        catch
        {
        }

        try
        {
            CancelToken.Dispose();
        }
        catch
        {
        }

        IsOnline = false;
    }

    public void LogPacket(string sendOrRecv, ushort opcode, byte[] payload)
    {
        if (!ConfigManager.Config.ServerOption.LogOption.EnableGamePacketLog) return;

        try
        {
            if (IgnoreLog.Contains(opcode)) return;

            if (ConfigManager.Config.ServerOption.LogOption.DisableLogDetailPacket) throw new Exception();

            var asJson = PacketLogHelper.ConvertPacketToJson(opcode, payload);
            var output = $"{sendOrRecv}: {LogMap[opcode]}({opcode})\r\n{asJson}";

            if (ConfigManager.Config.ServerOption.LogOption.LogPacketToConsole)
                Logger.Debug(output);

            if (DebugFile == "" || !ConfigManager.Config.ServerOption.LogOption.SavePersonalDebugFile) return;
            var sw = GetWriter();
            sw.WriteLine($"[{DateTime.Now:HH:mm:ss}] [GameServer] [DEBUG] " + output);
            sw.Flush();
        }
        catch
        {
            var output = $"{sendOrRecv}: {LogMap.GetValueOrDefault(opcode, "UnknownPacket")}({opcode})";

            if (ConfigManager.Config.ServerOption.LogOption.LogPacketToConsole)
                Logger.Debug(output);

            if (DebugFile != "" && ConfigManager.Config.ServerOption.LogOption.SavePersonalDebugFile)
            {
                var sw = GetWriter();
                sw.WriteLine($"[{DateTime.Now:HH:mm:ss}] [GameServer] [DEBUG] " + output);
                sw.Flush();
            }
        }
    }

    private StreamWriter GetWriter()
    {
        var file = new FileInfo(DebugFile);
        if (!file.Exists)
        {
            Directory.CreateDirectory(file.DirectoryName!);
            File.Create(DebugFile).Dispose();
        }

        Writer ??= new StreamWriter(DebugFile, true);
        return Writer;
    }

    public async Task SendPacket(byte[] packet)
    {
        try
        {
            if (ConfigManager.Config.GameServer.UsePacketEncryption)
                Crypto.Xor(packet, XorKey!);

            await _writeLock.WaitAsync(CancelToken.Token);
            try
            {
                await Stream.WriteAsync(packet, CancelToken.Token);
                await Stream.FlushAsync(CancelToken.Token);
            }
            finally
            {
                _writeLock.Release();
            }
        }
        catch
        {
            // ignore
        }
    }

    public async Task SendPacket(BasePacket packet)
    {
        if (packet.CmdId <= 0)
        {
            Logger.Debug("Tried to send packet with missing cmd id!");
            return;
        }

        if (BannedPackets.Contains(packet.CmdId)) return;
        LogPacket("Send", packet.CmdId, packet.Data);
        var packetBytes = packet.BuildPacket();

        try
        {
            await SendPacket(packetBytes);
        }
        catch
        {
            // ignore
        }

        if (packet.CmdId == CmdIds.SetClientPausedScRsp)
        {
            await SendWatermarkLuaAsync();
        }
    }

    public async Task SendPacket(int cmdId)
    {
        await SendPacket(new BasePacket((ushort)cmdId));
    }

    public virtual Task SendWatermarkLuaAsync() => Task.CompletedTask;
}
