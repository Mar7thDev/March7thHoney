using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using March7thHoney.Configuration;
using March7thHoney.Data.Custom;
using March7thHoney.Enums.Server;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server.Packet;
using March7thHoney.Kcp;
using March7thHoney.Proto;
using March7thHoney.Util;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace March7thHoney.GameServer.Server;

public class Connection(TcpClient client, IPEndPoint remote, long conversationId)
    : March7thHoneyConnection(client, remote, conversationId)
{
    private static readonly Logger Logger = new("GameServer");

    public PlayerInstance? Player { get; set; }
    public List<PacketActionData> CustomPacketQueue { get; set; } = [];

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

        // TODO 4.3: TrainCakeCatchManager 暂停
        Player?.OnLogoutAsync();
        March7thHoneyListener.UnregisterConnection(this);
        base.Stop();
    }

    public override async Task SendWatermarkLuaAsync()
    {
        if (Player == null) return;
        var watermark = ConfigManager.Config.ServerOption.Watermark;
        if (!watermark.Enabled) return;

        var nickname = Player.Data.Name ?? "";
        var uid = Player.Uid.ToString();
        var version = string.IsNullOrEmpty(ClientVersionCache.LatestVersion)
            ? $"WIN{GameConstants.GAME_VERSION}"
            : ClientVersionCache.LatestVersion;

        var line1 = BuildWatermarkLine(watermark.Line1Template, nickname, uid, version, watermark);
        var line2 = BuildWatermarkLine(watermark.Line2Template, nickname, uid, version, watermark);

        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(line1)) lines.Add(line1);
        if (!string.IsNullOrWhiteSpace(line2)) lines.Add(line2);
        if (lines.Count == 0) return;

        var luaText = EscapeLuaString(string.Join("\n", lines));
        var fontSize = watermark.FontSize.ToString(CultureInfo.InvariantCulture);

        var lua =
            "local function modify_texts()\n" +
            "    local vGo = CS.UnityEngine.GameObject.Find(\"VersionText\")\n" +
            "    if vGo == nil then return end\n" +
            "    local vt = vGo:GetComponent(\"Text\")\n" +
            "    vt.supportRichText = true\n" +
            "    vt.text = \"" + luaText + "\"\n" +
            "    vt.fontSize = " + fontSize + "\n" +
            "end\n" +
            "\n" +
            "local function on_error(err)\n" +
            "    local files = io.open(\"./error.txt\", \"w\")\n" +
            "    files:write(err)\n" +
            "    files:close()\n" +
            "end\n" +
            "\n" +
            "local status, err = pcall(modify_texts)\n" +
            "if not status then on_error(err) end\n";

        var pkt = new HandshakePacket(System.Text.Encoding.UTF8.GetBytes(lua));
        await SendPacket(pkt.BuildPacket());
    }

    private static string BuildWatermarkLine(string template, string nickname, string uid, string version,
        WatermarkConfig watermark)
    {
        var line = template
            .Replace("{nickname}", nickname, StringComparison.Ordinal)
            .Replace("{uid}", uid, StringComparison.Ordinal)
            .Replace("{version}", version, StringComparison.Ordinal)
            .Replace("{serverName}", ConfigManager.Config.GameServer.GameServerName, StringComparison.Ordinal)
            .Replace("{serverId}", ConfigManager.Config.GameServer.GameServerId, StringComparison.Ordinal);

        if (!watermark.UseGradient) return line;
        return ApplyGradient(line, watermark.GradientStartColor, watermark.GradientEndColor);
    }

    private static string ApplyGradient(string line, string startColor, string endColor)
    {
        if (!TryParseHexColor(startColor, out var r0, out var g0, out var b0) ||
            !TryParseHexColor(endColor, out var r1, out var g1, out var b1))
        {
            return line;
        }

        var visible = 0;
        foreach (var c in line)
            if (c != ' ') visible++;

        var sb = new System.Text.StringBuilder(line.Length * 24);
        var idx = 0;
        foreach (var c in line)
        {
            if (c == ' ')
            {
                sb.Append(' ');
                continue;
            }
            var t = visible <= 1 ? 0.0 : (double)idx / (visible - 1);
            var r = (int)(r0 + (r1 - r0) * t);
            var g = (int)(g0 + (g1 - g0) * t);
            var b = (int)(b0 + (b1 - b0) * t);
            sb.Append($"<color=#{r:X2}{g:X2}{b:X2}>").Append(c).Append("</color>");
            idx++;
        }
        return sb.ToString();
    }

    private static bool TryParseHexColor(string value, out int r, out int g, out int b)
    {
        r = g = b = 0;
        var hex = value.Trim();
        if (hex.StartsWith("#", StringComparison.Ordinal))
            hex = hex[1..];

        if (hex.Length != 6) return false;
        if (!int.TryParse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r)) return false;
        if (!int.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g))
            return false;
        return int.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b);
    }

    private static string EscapeLuaString(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
    }

    // 4.3 TCP packet 帧格式：
    //   [head_magic u32 BE = 0x9D74C714]
    //   [cmd_id u16 BE]
    //   [head_len u16 BE]
    //   [body_len u32 BE]
    //   [head bytes...]
    //   [body bytes...]
    //   [tail_magic u32 BE = 0xD7A152C8]
    protected async Task ReceiveLoop()
    {
        var headerBuf = new byte[12];
        var tailBuf = new byte[4];
        try
        {
            while (!CancelToken.IsCancellationRequested)
            {
                try
                {
                    await ReadExactlyAsync(headerBuf, 12);
                }
                catch (EndOfStreamException) { break; }
                catch (OperationCanceledException) when (CancelToken.IsCancellationRequested) { break; }
                catch (ObjectDisposedException) { break; }
                catch (IOException) { break; }

                var magic1 = BinaryPrimitives.ReadUInt32BigEndian(headerBuf.AsSpan(0, 4));
                if (magic1 != 0x9D74C714)
                {
                    Logger.Error($"Bad head magic: got 0x{magic1:X8}, expect 0x9D74C714");
                    break;
                }

                var opcode = BinaryPrimitives.ReadUInt16BigEndian(headerBuf.AsSpan(4, 2));
                int headerLength = BinaryPrimitives.ReadUInt16BigEndian(headerBuf.AsSpan(6, 2));
                var payloadLength = BinaryPrimitives.ReadUInt32BigEndian(headerBuf.AsSpan(8, 4));
                if (payloadLength > 0xFFFFFF)
                {
                    Logger.Error($"Payload too big: {payloadLength}");
                    break;
                }

                var header = headerLength > 0 ? new byte[headerLength] : [];
                var payload = payloadLength > 0 ? new byte[payloadLength] : [];

                try
                {
                    if (headerLength > 0) await ReadExactlyAsync(header, headerLength);
                    if (payloadLength > 0) await ReadExactlyAsync(payload, (int)payloadLength);
                    await ReadExactlyAsync(tailBuf, 4);
                }
                catch (EndOfStreamException) { break; }
                catch (OperationCanceledException) when (CancelToken.IsCancellationRequested) { break; }
                catch (IOException) { break; }

                var magic2 = BinaryPrimitives.ReadUInt32BigEndian(tailBuf);
                if (magic2 != 0xD7A152C8)
                {
                    Logger.Error($"Bad tail magic: got 0x{magic2:X8}, expect 0xD7A152C8");
                    break;
                }

                try
                {
                    LogPacket("Recv", opcode, payload);
                    await HandlePacket(opcode, header, payload);
                }
                catch (Exception ex)
                {
                    Logger.Error("Handler error", ex);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Connection receive loop crashed", ex);
        }

        Stop();
    }

    private async Task ReadExactlyAsync(byte[] buffer, int count)
    {
        var read = 0;
        while (read < count)
        {
            var n = await Stream.ReadAsync(buffer.AsMemory(read, count - read), CancelToken.Token);
            if (n == 0) throw new EndOfStreamException();
            read += n;
        }
    }

    private async Task HandlePacket(ushort opcode, byte[] header, byte[] payload)
    {
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
                        var sendPacket = new BasePacket((ushort)LogMap.FirstOrDefault(x =>
                            x.Value == action.Param.PacketName).Key);

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
            switch (opcode)
            {
                case CmdIds.PlayerGetTokenCsReq:
                {
                    if (state != SessionStateEnum.WAITING_FOR_TOKEN) return;
                    goto default;
                }
                case CmdIds.PlayerLoginCsReq:
                {
                    if (state != SessionStateEnum.WAITING_FOR_LOGIN) return;
                    goto default;
                }
                default:
                    break;
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
                if (curPacket == null) return;

                var rspName = curPacket.Replace("Cs", "Sc").Replace("Req", "Rsp"); // Get the response packet name
                if (rspName == curPacket) return; // do not send rsp when resp name = recv name
                if (!TryGetOpcodeByName(rspName, out var rspOpcode)) return;

                // get proto class
                var typ = AppDomain.CurrentDomain.GetAssemblies()
                    .SingleOrDefault(assembly => assembly.GetName().Name == "March7thHoney.Proto")!.GetTypes()
                    .First(t => t.Name == rspName); //get the type using the packet name
                var curTyp = AppDomain.CurrentDomain.GetAssemblies()
                    .SingleOrDefault(assembly => assembly.GetName().Name == "March7thHoney.Proto")!.GetTypes()
                    .First(t => t.Name == curPacket); //get the type using the packet name

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
        // get the packet name
        if (packetName == null) return;

        var respName = packetName.Replace("Cs", "Sc").Replace("Req", "Rsp"); // Get the response packet name
        if (respName == packetName) return; // do not send rsp when resp name = recv name
        if (!TryGetOpcodeByName(respName, out var respOpcode)) return;

        // Send Rsp
        await SendPacket(respOpcode);
    }

    private static bool TryGetOpcodeByName(string packetName, out int opcode)
    {
        var match = LogMap.FirstOrDefault(x => x.Value == packetName);
        opcode = match.Key;
        return match.Value != null && opcode > 0;
    }
}
