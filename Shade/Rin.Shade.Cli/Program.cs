using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Rin.Shade.Transpiler;

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

return args[0] switch
{
    "compile" => RunCompile(args),
    _ => RunUnknown()
};

static int RunUnknown()
{
    PrintUsage();
    return 1;
}

static int RunCompile(string[] args)
{
    List<string> inputPaths = [];
    string? outputPath = null;
    List<string> referencePaths = [];

    for (var i = 1; i < args.Length; i++)
    {
        var arg = args[i];
        switch (arg)
        {
            case "-o":
            case "--output":
                outputPath = args[++i];
                break;
            case "-r":
            case "--reference":
                referencePaths.Add(args[++i]);
                break;
            default:
                inputPaths.Add(arg);
                break;
        }
    }

    if (inputPaths.Count == 0)
    {
        PrintUsage();
        return 1;
    }

    outputPath ??= Path.ChangeExtension(inputPaths[0], ".slang");

    var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Concat(referencePaths)
        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
        .Append(MetadataReference.CreateFromFile(typeof(Rin.Shade.Shader).Assembly.Location));

    var syntaxTrees = inputPaths.Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path));
    var compilation = CSharpCompilation.Create("RinShadeCli", syntaxTrees, references,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    var result = ShadeEmitter.Emit(compilation);

    var hadError = false;
    foreach (var diagnostic in result.Diagnostics)
    {
        Console.Error.WriteLine(diagnostic.ToString());
        if (diagnostic.Severity == DiagnosticSeverity.Error) hadError = true;
    }

    if (result.Shaders.Count == 0)
    {
        Console.Error.WriteLine($"No [Shader]-derived class found in {string.Join(", ", inputPaths)}");
        return 1;
    }

    foreach (var (name, slang) in result.Shaders)
    {
        var shaderOutputPath = result.Shaders.Count == 1
            ? outputPath
            : Path.Combine(Path.GetDirectoryName(outputPath) ?? ".",
                $"{Path.GetFileNameWithoutExtension(outputPath)}.{name}.slang");

        var directory = Path.GetDirectoryName(shaderOutputPath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        File.WriteAllText(shaderOutputPath, slang);
        Console.WriteLine($"Compiled '{name}' -> '{shaderOutputPath}'");
    }

    return hadError ? 1 : 0;
}

static void PrintUsage()
{
    Console.Error.WriteLine("""
                             Usage:
                               rin-shade compile <input.cs>... -o <output.slang> [-r <reference.dll>]...
                             """);
}
