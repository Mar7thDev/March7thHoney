using March7thHoney.SourceGen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

// Runs each source generator on a small input and compares what it emits with the checked-in
// expectation, so a change to a generator shows up as a reviewed diff of generated code.
//
//   dotnet run --project scripts/SourceGenRegression/SourceGenRegression.csproj -c Release
//   dotnet run --project scripts/SourceGenRegression/SourceGenRegression.csproj -c Release -- --update
//
// --update rewrites the expectations from the current generators; review the diff before committing.
// Each case is a directory under Cases/ with an Input.cs and the files the generator must emit.

var update = args.Contains("--update");
var casesRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../Cases"));
var checks = 0;
var failures = 0;

void Check(string name, bool passed, string? detail = null)
{
    checks++;
    if (!passed) failures++;
    Console.WriteLine($"{(passed ? "PASS" : "FAIL")} {name}{(passed || detail == null ? "" : $": {detail}")}");
}

// The generated samples are compiled against everything this program itself can load.
var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
    .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
    .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
    .ToList();
var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
    nullableContextOptions: NullableContextOptions.Enable);

static string Normalize(string text) => text.Replace("\r\n", "\n");

CSharpCompilation Compile(string caseName, string source) => CSharpCompilation.Create(
    "Sample" + caseName,
    [CSharpSyntaxTree.ParseText(source, parseOptions, path: "Input.cs")],
    references,
    compilationOptions);

void RunCase(string caseName, IIncrementalGenerator generator)
{
    var directory = Path.Combine(casesRoot, caseName);
    var input = File.ReadAllText(Path.Combine(directory, "Input.cs"));
    var compilation = Compile(caseName, input);

    var inputErrors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
    Check($"{caseName}: the input compiles on its own", inputErrors.Count == 0,
        string.Join("; ", inputErrors.Take(3)));

    var driver = CSharpGeneratorDriver.Create(generator)
        .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);
    Check($"{caseName}: the generator reports nothing", generatorDiagnostics.IsEmpty,
        string.Join("; ", generatorDiagnostics.Take(3)));

    var outputErrors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
    Check($"{caseName}: the generated code compiles", outputErrors.Count == 0,
        string.Join("; ", outputErrors.Take(3)));

    var generated = driver.GetRunResult().Results.Single().GeneratedSources
        .ToDictionary(s => s.HintName, s => Normalize(s.SourceText.ToString()));
    var expectedFiles = Directory.EnumerateFiles(directory, "*.g.cs").Select(Path.GetFileName).OfType<string>().ToList();

    if (update)
    {
        foreach (var stale in expectedFiles.Where(name => !generated.ContainsKey(name)))
            File.Delete(Path.Combine(directory, stale));
        foreach (var (name, text) in generated)
            File.WriteAllText(Path.Combine(directory, name), text);
        Console.WriteLine($"UPDATED {caseName}: {string.Join(", ", generated.Keys.Order())}");
        return;
    }

    Check($"{caseName}: emits exactly the expected files",
        generated.Keys.Order().SequenceEqual(expectedFiles.Order()),
        $"emitted [{string.Join(", ", generated.Keys.Order())}], expected [{string.Join(", ", expectedFiles.Order())}]");
    foreach (var name in expectedFiles.Where(generated.ContainsKey))
    {
        var expected = Normalize(File.ReadAllText(Path.Combine(directory, name)));
        var actual = generated[name];
        Check($"{caseName}: {name} matches the expectation", actual == expected, FirstDifference(expected, actual));
    }
}

static string FirstDifference(string expected, string actual)
{
    var expectedLines = expected.Split('\n');
    var actualLines = actual.Split('\n');
    for (var i = 0; i < Math.Max(expectedLines.Length, actualLines.Length); i++)
    {
        var e = i < expectedLines.Length ? expectedLines[i] : "<end of file>";
        var a = i < actualLines.Length ? actualLines[i] : "<end of file>";
        if (e != a) return $"line {i + 1}: expected '{e.Trim()}', generated '{a.Trim()}'";
    }
    return "";
}

RunCase("Handlers", new HandlerRegistryGenerator());
RunCase("Missions", new MissionHandlerRegistryGenerator());
RunCase("Commands", new CommandRegistryGenerator());
RunCase("MemoryPack", new MemoryPackRegistrarGenerator());
RunCase("Database", new DatabaseStoreGenerator());

Console.WriteLine($"SourceGen regression checks: {checks}, failures: {failures}");
return failures == 0 ? 0 : 1;
