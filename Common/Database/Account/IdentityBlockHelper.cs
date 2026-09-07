using System.Collections.Concurrent;
using March7thHoney.Util;

namespace March7thHoney.Database.Account;

public static class IdentityBlockHelper
{
    private static readonly object SyncRoot = new();
    private static readonly ConcurrentDictionary<string, IdentityBlockData> Blocks = new(StringComparer.Ordinal);
    private static int _nextId = -1;

    public static void Initialize()
    {
        lock (SyncRoot)
        {
            Blocks.Clear();
            foreach (var block in DatabaseHelper.GetAllInstanceFromMap<IdentityBlockData>() ?? [])
                Blocks[block.IdentityKey] = block;
            if (!Blocks.IsEmpty) _nextId = Math.Min(_nextId, checked(Blocks.Values.Min(b => b.Uid) - 1));
        }
    }

    public static List<IdentityBlockData> GetSnapshot() => Blocks.Values.OrderByDescending(x => x.CreatedAt).ToList();

    public static bool TryGetActiveBan(IEnumerable<string>? keys, out AccountBanStatus status)
    {
        foreach (var key in keys ?? [])
            if (Blocks.TryGetValue(key, out var block) && block.GetStatus() is { IsActive: true } active)
            {
                status = active;
                return true;
            }
        status = AccountBanStatus.Inactive;
        return false;
    }

    public static IdentityBlockData Block(string kind, string value, string? reason, long expireAt,
        bool isIdentityKey = false)
    {
        if (DatabaseHelper.Instance == null) throw new InvalidOperationException("Database is not initialized");
        string? key;
        if (isIdentityKey)
        {
            key = AccountBanHelper.ParseIdentityKeys(value).SingleOrDefault();
            if (key != value || !value.StartsWith(kind + ":", StringComparison.Ordinal))
                throw new ArgumentException("Invalid identity key");
        }
        else
        {
            value = kind switch
            {
                "ip" => AccountBanHelper.NormalizeIpAddress(value) ?? "",
                "device" => AccountBanHelper.NormalizeDeviceValue(value) ?? "",
                _ => throw new ArgumentException("Kind must be ip or device")
            };
            key = kind == "ip" ? AccountBanHelper.CreateIpIdentityKey(value) : AccountBanHelper.CreateDeviceIdentityKey(value);
        }
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("Invalid IP address or device ID");
        if ((reason?.Length ?? 0) > 512) throw new ArgumentException("Reason is too long");

        lock (SyncRoot)
        {
            var existing = Blocks.GetValueOrDefault(key);
            if (existing != null)
            {
                // Keep a rule immutable for concurrent readers. Replace only after persistence succeeds.
                var replacement = new IdentityBlockData
                {
                    Uid = existing.Uid, IdentityKey = key, Kind = kind, Value = value,
                    Reason = reason?.Trim() ?? "", CreatedAt = Extensions.GetUnixSec(), ExpireAt = expireAt
                };
                DatabaseHelper.UpdateInstanceStrict(existing, replacement);
                Blocks[key] = replacement;
                return replacement;
            }

            var id = _nextId;
            _nextId = checked(_nextId - 1);
            var block = new IdentityBlockData
            {
                Uid = id, IdentityKey = key, Kind = kind, Value = value,
                Reason = reason?.Trim() ?? "", CreatedAt = Extensions.GetUnixSec(), ExpireAt = expireAt
            };
            DatabaseHelper.SaveInstance(block);
            Blocks[key] = block;
            return block;
        }
    }

    public static bool Remove(int id)
    {
        lock (SyncRoot)
        {
            var block = Blocks.Values.FirstOrDefault(x => x.Uid == id);
            if (block == null) return false;
            DatabaseHelper.Instance!.DeleteInstance(block);
            Blocks.TryRemove(block.IdentityKey, out _);
            return true;
        }
    }
}
