using March7thHoney.Data;
using March7thHoney.Database;
using March7thHoney.Database.Account;
using March7thHoney.Database.Player;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server.Packet.Send.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Server.Packet.Recv.Player;

[Opcode(CmdIds.PlayerGetTokenCsReq)]
public class HandlerPlayerGetTokenCsReq : Handler
{
    private readonly Logger _logger = new("GameServer");

    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = PlayerGetTokenCsReq.Parser.ParseFrom(data);

        // call dispatch /get_account_info api to get account info
        int uid;

        if (ConfigManager.Config.ServerOption.ServerConfig.RunDispatch ||
            string.IsNullOrEmpty(ConfigManager.Config.ServerOption.ServerConfig.FromDispatchBaseUrl))
        {
            // dispatch running, use local db
            var account = DatabaseHelper.Instance?.GetInstance<AccountData>(int.Parse(req.AccountUid));
            if (account == null)
            {
                await connection.SendPacket(new PacketPlayerGetTokenScRsp(0, Retcode.RetNotInWhiteList));
                return;
            }

            uid = account.Uid;
        }
        else
        {
            // dispatch not running, use dispatch api
            var dispatchUrl = ConfigManager.Config.ServerOption.ServerConfig.FromDispatchBaseUrl;
            var targetUrl = $"{dispatchUrl}/get_account_info?accountUid={req.AccountUid}";
            var res = await HttpNetwork.SendGetRequest(targetUrl);
            if (res.Item1 != 200 || res.Item2 == null)
            {
                await connection.SendPacket(new PacketPlayerGetTokenScRsp(0, Retcode.RetNotInWhiteList));
                return;
            }

            uid = int.Parse(res.Item2);
        }


        if (!ResourceManager.IsLoaded)
            // resource manager not loaded, return
            return;

        var prev = Listener.GetActiveConnection(uid);
        if (prev != null)
        {
            prev.Stop();
        }

        connection.State = SessionStateEnum.WAITING_FOR_LOGIN;

        var pd = DatabaseHelper.Instance?.GetInstance<PlayerData>(uid);
        connection.Player = pd == null ? new PlayerInstance(uid) : new PlayerInstance(pd);

        connection.DebugFile = Path.Combine(ConfigManager.Config.Path.LogPath, "Debug/", $"{uid}/",
            $"Debug-{DateTime.Now:yyyy-MM-dd HH-mm-ss}.log");

        await connection.Player.OnGetToken();
        connection.Player.Connection = connection;
        await connection.SendPacket(new PacketPlayerGetTokenScRsp(connection));

        if (ConfigManager.Config.GameServer.UsePacketEncryption)
        {
            connection.XorKey = Crypto.GenerateXorKey(connection.ClientSecretKeySeed);
            _logger.Info($"{connection.RemoteEndPoint} key exchange successful");
        }
    }
}

