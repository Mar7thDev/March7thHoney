using March7thHoney.Database.Account;

namespace March7thHoney.WebServer.Server;

public class MuipSession
{
    public string RsaPublicKey { get; set; } = "";
    public string SessionId { get; set; } = "";
    public long ExpireTimeStamp { get; set; } = 0;

    public bool IsAdmin { get; set; } = false;
    public bool IsAuthorized { get; set; } = false;
    // Internal-only credential binding; never send the administrator key or its fingerprint to clients.
    internal string AdminKeyFingerprint { get; } = MuipManager.GetAdminKeyFingerprint();
    internal string? ClientKey { get; init; }
    public AccountData? Account { get; set; } = null;
}