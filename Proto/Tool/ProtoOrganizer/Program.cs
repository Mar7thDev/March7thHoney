using March7thHoney.ProtoOrganizer;

// Maintains Proto/StarRail.proto, the one protocol definition the server is built from.
// Paths are relative to the current directory, normally the repository root.

const string Schema = "Proto/StarRail.proto";
const string Overrides = "Proto/overrides-manual.txt";
const string Aliases = "Proto/cmdid-aliases.txt";
const string CmdIds = "KcpSharp/CmdIds.cs";

const string Usage = """
    ProtoOrganizer carry --dump <new dump> --prev-dump <previous dump> [options]
      Moves the names of the current schema onto a newer Demiurge dump and writes the result.
        --schema <path>     schema to read the names from      (default Proto/StarRail.proto)
        --out <path>        where to write the new schema      (default: the --schema path)
        --overrides <path>  hand-checked names for this dump   (default Proto/overrides-manual.txt, if present)
        --report <path>     write what was added, removed and reshaped
        --label <text>      description of the new dump for the first line of the schema

    ProtoOrganizer cmdids [options]
      Regenerates the cmdId constants from the schema.
        --schema <path>     schema to read                     (default Proto/StarRail.proto)
        --aliases <path>    alias = constant, one per line     (default Proto/cmdid-aliases.txt, if present)
        --out <path>        file to write                      (default KcpSharp/CmdIds.cs)
        --check             write nothing; fail if the file is not what would be written
    """;

if (args.Length == 0) return Fail("no command given");

var values = new Dictionary<string, string>(StringComparer.Ordinal);
var check = false;
for (var i = 1; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--check":
            check = true;
            break;
        case "--dump" or "--prev-dump" or "--schema" or "--out" or "--overrides" or "--report" or "--label" or "--aliases":
            if (i + 1 >= args.Length) return Fail($"{args[i]} needs a value");
            values[args[i]] = args[++i];
            break;
        default:
            return Fail($"unknown argument: {args[i]}");
    }
}

string? Value(string name) => values.GetValueOrDefault(name);
string? Existing(string name, string fallback) => Value(name) ?? (File.Exists(fallback) ? fallback : null);

switch (args[0])
{
    case "carry":
        if (Value("--dump") is not { } dump || Value("--prev-dump") is not { } prevDump)
            return Fail("carry needs --dump and --prev-dump");
        var schema = Value("--schema") ?? Schema;
        return CarryCommand.Run(new CarryOptions(
            dump, prevDump, schema, Value("--out") ?? schema,
            Existing("--overrides", Overrides), Value("--report"), Value("--label")));
    case "cmdids":
        return CmdIdsCommand.Run(new CmdIdsOptions(
            Value("--schema") ?? Schema, Existing("--aliases", Aliases), Value("--out") ?? CmdIds, check));
    default:
        return Fail($"unknown command: {args[0]}");
}

int Fail(string message)
{
    Console.Error.WriteLine($"[organizer] {message}");
    Console.Error.WriteLine();
    Console.Error.WriteLine(Usage);
    return 2;
}
