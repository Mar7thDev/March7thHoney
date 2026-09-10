using System.Collections.Concurrent;
using March7thHoney.Database.Account;
using March7thHoney.Util;
using March7thHoney.Util.Security;

namespace March7thHoney.WebServer.Handler;

/// <summary>
///     Short-lived sessions for the player self-service console (/player). A session is bound to one account
///     uid, so the page sends the password once instead of attaching it to every command — the launcher's older
///     uid+password calls keep working next to this, see <see cref="Controllers.ConsoleRoutes" />.
/// </summary>
public static class PlayerConsoleSessionManager
{
    private static readonly ConcurrentDictionary<string, PlayerConsoleSession> Sessions = [];
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(2);

    public static PlayerConsoleSession Create(AccountData account)
    {
        CleanupExpiredSessions();

        var session = new PlayerConsoleSession(
            AuthSecurity.GenerateSessionToken(),
            account.Uid,
            DateTimeOffset.UtcNow.Add(SessionLifetime));

        Sessions[session.Token] = session;
        return session;
    }

    /// <summary>Resolve a token to a live session and its account; expired or unknown tokens fail.</summary>
    public static bool TryResolve(string? token, out PlayerConsoleSession session, out AccountData account)
    {
        session = default;
        account = null!;

        if (!TryGetSession(token, out session))
            return false;

        var resolved = AccountData.GetAccountByUid(session.Uid);
        if (resolved == null)
        {
            Sessions.TryRemove(session.Token, out _);
            return false;
        }

        account = resolved;
        return true;
    }

    public static void Revoke(string? token)
    {
        if (!string.IsNullOrWhiteSpace(token))
            Sessions.TryRemove(token, out _);
    }

    /// <summary>Drop every session of one account — used after a self-reset so stale state cannot be reused.</summary>
    public static void RevokeForUid(int uid)
    {
        foreach (var (token, session) in Sessions)
        {
            if (session.Uid == uid)
                Sessions.TryRemove(token, out _);
        }
    }

    private static bool TryGetSession(string? token, out PlayerConsoleSession session)
    {
        CleanupExpiredSessions();
        session = default;

        if (string.IsNullOrWhiteSpace(token))
            return false;

        if (!Sessions.TryGetValue(token, out var existing))
            return false;

        if (existing.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            Sessions.TryRemove(token, out _);
            return false;
        }

        session = existing;
        return true;
    }

    private static void CleanupExpiredSessions()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var (token, session) in Sessions)
        {
            if (session.ExpiresAt <= now)
                Sessions.TryRemove(token, out _);
        }
    }

    public readonly record struct PlayerConsoleSession(string Token, int Uid, DateTimeOffset ExpiresAt);
}
