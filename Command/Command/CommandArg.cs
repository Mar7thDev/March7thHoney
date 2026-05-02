using March7thHoney.GameServer.Server;
using March7thHoney.Kcp;
using March7thHoney.Util;

namespace March7thHoney.Command.Command;

public class CommandArg
{
    public CommandArg(string raw, ICommandSender sender, Connection? con = null)
    {
        Raw = raw;
        Sender = sender;
        var args = raw.Split(' ');
        foreach (var arg in args)
        {
            if (string.IsNullOrEmpty(arg)) continue;
            var character = arg[0];
            if (!int.TryParse(character.ToString(), out _) && character != '-')
            {
                try
                {
                    CharacterArgs.Add(arg[..1], arg[1..]);
                    Args.Add(arg);
                }
                catch
                {
                    BasicArgs.Add(arg);
                    Args.Add(arg);
                }
            }
            else
            {
                BasicArgs.Add(arg);
                Args.Add(arg);
            }
        }

        if (con != null) Target = con;

        CharacterArgs.TryGetValue("@", out var target);
        if (target == null) return;
        if (March7thHoneyListener.Connections.Values.ToList()
                .Find(item => (item as Connection)?.Player?.Uid.ToString() == target) is Connection connection)
            Target = connection;
    }

    public string Raw { get; }
    public List<string> Args { get; } = [];
    public List<string> BasicArgs { get; } = [];
    public Dictionary<string, string> CharacterArgs { get; } = [];
    public Connection? Target { get; set; }
    public ICommandSender Sender { get; }

    public int GetInt(int index)
    {
        if (BasicArgs.Count <= index) return 0;
        _ = int.TryParse(BasicArgs[index], out var res);
        return res;
    }

    public async ValueTask SendMsg(string msg)
    {
        await Sender.SendMsg(msg);
    }

    public override string ToString()
    {
        return $"BasicArg: {BasicArgs.ToArrayString()}. CharacterArg: {CharacterArgs.ToJsonString()}.";
    }
}