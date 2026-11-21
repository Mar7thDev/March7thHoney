using System.Buffers;
using System.Net;
using System.Reflection;
using March7thHoney.Database;
using March7thHoney.Data.Custom;
using March7thHoney.Enums.Server;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server.ClientLua;
using March7thHoney.GameServer.Server.Packet;
using March7thHoney.Kcp;
using March7thHoney.Kcp.KcpSharp;
using March7thHoney.Proto;
using March7thHoney.Util;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace March7thHoney.GameServer.Server;

public class Connection : March7thHoneyConnection
{
    private static readonly Logger Logger = new("GameServer");

    public Connection(KcpConversation conversation, IPEndPoint remote) : base(conversation, remote)
    {
        ClientLua = new ClientLuaSession(this);
    }

    public PlayerInstance? Player { get; set; }
    public List<PacketActionData> CustomPacketQueue { get; set; } = [];
    public ClientLuaSession ClientLua { get; }

    private PacketActionData? GetCurActionData()
    {
        if (CustomPacketQueue.Count == 0) return null;
        var actionData = CustomPacketQueue[0];

        return actionData;
    }

    public override async void Start()
    {
        Logger.Info($"New connection from {RemoteEndPoint}.");
        State = SessionStateEnum.WAITING_FOR_TOKEN;
        await ReceiveLoop();
    }

    public override void Stop()
    {
        if (!IsOnline) return;

        Player?.TrainCakeCatchManager?.LeaveSocialPlayRoom();
        Player?.OnLogout();
        March7thHoneyListener.UnregisterConnection(this);
        base.Stop();
    }

    protected async Task ReceiveLoop()
    {
        try
        {
            while (!CancelToken.IsCancellationRequested)
            {
                KcpConversationReceiveResult result;
                try
                {
                    // WaitToReceiveAsync call completes when there is at least one message is received or the transport is closed.
                    result = await Conversation.WaitToReceiveAsync(CancelToken.Token);
                }
                catch (OperationCanceledException) when (CancelToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                if (result.TransportClosed)
                {
                    Logger.Debug("Connection was closed");
                    break;
                }

                var buffer = ArrayPool<byte>.Shared.Rent(result.BytesReceived);
                try
                {
                    // TryReceive should not return false here, unless the transport is closed.
                    // So we don't need to check for result.TransportClosed.
                    if (!Conversation.TryReceive(buffer, out result))
                    {
                        Logger.Error("Failed to receive packet");
                        break;
                    }

                    await ProcessMessageAsync(buffer.AsMemory(0, result.BytesReceived));
                }
                catch (Exception ex)
                {
                    Logger.Error("Packet parse error", ex);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Connection receive loop crashed", ex);
        }

        Stop();
    }

    // DO THE PROCESSING OF THE GAME PACKET
    private async Task ProcessMessageAsync(Memory<byte> data)
    {
        var gamePacket = data.ToArray();
        if (ConfigManager.Config.GameServer.UsePacketEncryption)
            Crypto.Xor(gamePacket, XorKey!);

        await using MemoryStream ms = new(gamePacket);
        using BinaryReader br = new(ms);

        // Handle
        try
        {
            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                // Length
                if (br.BaseStream.Length - br.BaseStream.Position < BasePacket.MinFrameBytes) return;
                // Packet sanity check
                var magic1 = br.ReadUInt32BE();
                if (magic1 != BasePacket.HeaderMagic)
                {
                    Logger.Error($"Bad Data Package Received: got 0x{magic1:X}, expect 0x{BasePacket.HeaderMagic:X}");
                    return; // Bad packet
                }

                // Data
                var opcode = br.ReadUInt16BE();
                var headerLength = br.ReadUInt16BE();
                var payloadLength = br.ReadUInt32BE();

                // Reject claimed lengths that exceed what's actually left in the buffer before
                // calling ReadBytes — BinaryReader.ReadBytes allocates the full requested array
                // up front, so an unchecked huge payloadLength is a remote memory-exhaustion vector.
                var remaining = br.BaseStream.Length - br.BaseStream.Position;
                if (headerLength + (long)payloadLength > remaining)
                {
                    Logger.Error(
                        $"Bad Data Package Received: header/payload length ({headerLength}+{payloadLength}) exceeds remaining buffer ({remaining})");
                    return;
                }

                var header = br.ReadBytes(headerLength);
                var payload = br.ReadBytes((int)payloadLength);
                LogPacket("Recv", opcode, payload);
                await HandlePacket(opcode, header, payload);
            }
        }
        catch (Exception e)
        {
            Logger.Error(e.Message, e);
        }
    }

    private async Task HandlePacket(ushort opcode, byte[] header, byte[] payload)
    {
        // Scoped to this request: everything sent while handling it echoes its packet id (see PacketHead).
        PacketHead.RequestPacketId.Value = PacketHead.ReadPacketId(header);
        if (Player != null && DatabaseHelper.IsUidDeleted(Player.Uid))
        {
            Stop();
            return;
        }
        // check if it's a custom packet
        var action = GetCurActionData();
        var packetName = LogMap.GetValueOrDefault(opcode);

        if (action is { Action: PacketActionTypeEnum.WaitForPacket } && action.Param.PacketName == packetName)
        {
            // while run action
            var interrupt = action.Param.InterruptFormalHandler;
            while (true)
            {
                switch (action.Action)
                {
                    case PacketActionTypeEnum.SendPacket:
                    {
                        var sendPacket = new BasePacket(
                            TryGetCmdIdByName(action.Param.PacketName, out var actionCmdId)
                                ? (ushort)actionCmdId
                                : (ushort)0);

                        sendPacket.SetData(action.Param.PacketData);

                        await SendPacket(sendPacket);
                        break;
                    }
                    case PacketActionTypeEnum.WaitForPacket:
                    {
                        break;
                    }
                }

                CustomPacketQueue.RemoveAt(0);
                action = GetCurActionData();
                if (action == null || action.Action == PacketActionTypeEnum.WaitForPacket) break;
            }

            if (interrupt)
                return;
        }

        // Find the Handler for this opcode
        var handler = HandlerManager.GetHandler(opcode);
        if (handler != null)
        {
            // Handle
            // Make sure session is ready for packets
            var state = State;
            switch (state)
            {
                case SessionStateEnum.WAITING_FOR_TOKEN when opcode != CmdIds.PlayerGetTokenCsReq:
                case SessionStateEnum.WAITING_FOR_LOGIN when opcode != CmdIds.PlayerLoginCsReq:
                case SessionStateEnum.ACTIVE
                    when opcode == CmdIds.PlayerGetTokenCsReq || opcode == CmdIds.PlayerLoginCsReq:
                    return;
            }

            try
            {
                await handler.OnHandle(this, header, payload);
            }
            catch (Exception e)
            {
                Logger.Error("An error occured ", e);

                // get the packet rsp and set retCode to Retcode.RetFail
                var curPacket = LogMap.GetValueOrDefault(opcode);
                if (curPacket == null || !TryDeriveResponseName(curPacket, out var rspName)) return;
                if (!TryGetCmdIdByName(rspName, out var rspOpcode)) return;

                // get proto class
                // Because of the cmdid aliases (cmdid-aliases.txt), a name in LogMap may not be a message that really
                // exists in the proto. If a type is not found, give up on the fallback reply: another exception thrown
                // here would take down the receive loop.
                var protoTypes = ProtoTypeMap.Value;
                if (!protoTypes.TryGetValue(rspName, out var typ) ||
                    !protoTypes.TryGetValue(curPacket, out var curTyp)) return;

                // create the response packet
                if (Activator.CreateInstance(typ) is not IMessage rsp) return;

                // set the retCode to Retcode.RetFail
                var retCode = typ.GetProperty("Retcode");
                retCode?.SetValue(rsp, (uint)Retcode.RetFail);

                // get the same field in req and rsp
                var descriptor =
                    curTyp.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static)?.GetValue(
                        null, null) as MessageDescriptor; // get the static property Descriptor
                var reqPacket = descriptor?.Parser.ParseFrom(payload);

                foreach (var propertyInfo in curTyp.GetProperties())
                {
                    var prop = typ.GetProperty(propertyInfo.Name);
                    if (prop != null && prop.CanWrite)
                    {
                        var value = propertyInfo.GetValue(reqPacket);
                        if (value != null)
                            prop.SetValue(rsp, value);
                    }
                }

                // send the response packet
                var packet = new BasePacket((ushort)rspOpcode);
                packet.SetData(rsp);
                await SendPacket(packet);
            }

            return;
        }

        // No handler found
        // Any response to a tutorial request makes the client pop up a tutorial screen that should not appear. The
        // csproj excludes these three handlers for the same reason.
        if (opcode == CmdIds.GetTutorialCsReq || opcode == CmdIds.GetTutorialGuideCsReq ||
            opcode == CmdIds.UnlockTutorialCsReq) return;

        // get the packet name
        if (packetName == null || !TryDeriveResponseName(packetName, out var respName)) return;
        if (!TryGetCmdIdByName(respName, out var respOpcode)) return;

        // Send Rsp
        await SendPacket(respOpcode);
    }

    /// <summary>GetXxxCsReq → GetXxxScRsp; false when the name carries no Cs/Req markers (notifies etc.).</summary>
    private static bool TryDeriveResponseName(string requestName, out string responseName)
    {
        responseName = requestName.Replace("Cs", "Sc").Replace("Req", "Rsp");
        return responseName != requestName;
    }

    // Name → proto message type, built once so the RetFail fallback does not enumerate the Proto assembly on
    // every handler exception.
    private static readonly Lazy<Dictionary<string, Type>> ProtoTypeMap = new(() =>
    {
        try
        {
            return typeof(PlayerGetTokenCsReq).Assembly.GetTypes()
                .GroupBy(t => t.Name)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        }
        catch (Exception e)
        {
            // A type that fails to load (trimmed publish) must not take down the receive loop; the fallback
            // replies are disabled instead.
            Logger.Warn($"Proto type map unavailable, fallback replies disabled: {e.Message}");
            return new Dictionary<string, Type>(StringComparer.Ordinal);
        }
    });

    // The 4.x client repaints its version/watermark overlay after the SetClientPaused answer lands.
    protected override async Task OnPacketSentAsync(BasePacket packet)
    {
        if (packet.CmdId == CmdIds.SetClientPausedScRsp)
            await ClientLua.SendClientUiAsync();
    }
}
