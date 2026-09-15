using Rin.Slang;
using Rin.Slang.Compiler;
using Rin.Slang.Discovery;

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

return args[0] switch
{
    "compile" => RunCompile(args),
    "discover" => RunDiscover(args),
    "compile-referenced" => RunCompileReferenced(args),
    _ => RunUnknown()
};

static int RunUnknown()
{
    PrintUsage();
    return 1;
}

static int RunCompile(string[] args)
{
    string? inputPath = null;
    string? outputPath = null;
    var options = new ShaderCompilerOptions();

    for (var i = 1; i < args.Length; i++)
    {
        var arg = args[i];
        switch (arg)
        {
            case "-o":
            case "--output":
                outputPath = args[++i];
                break;
            case "-I":
            case "--include":
                options.AddSearchPath(args[++i]);
                break;
            case "-D":
            case "--define":
            {
                if (!TrySplitPair(args[++i], out var name, out var value))
                {
                    Console.Error.WriteLine($"Invalid -D value '{args[i]}', expected NAME=VALUE");
                    return 1;
                }

                options.AddDefine(name, value);
                break;
            }
            case "-A":
            case "--alias":
            {
                if (!TrySplitPair(args[++i], out var alias, out var path))
                {
                    Console.Error.WriteLine($"Invalid -A value '{args[i]}', expected ALIAS=PATH");
                    return 1;
                }

                options.AddPathAlias(alias, path);
                break;
            }
            default:
                if (inputPath != null)
                {
                    Console.Error.WriteLine($"Unexpected argument '{arg}'");
                    return 1;
                }

                inputPath = arg;
                break;
        }
    }

    if (inputPath == null)
    {
        PrintUsage();
        return 1;
    }

    outputPath ??= Path.ChangeExtension(inputPath, ".crsh");

    try
    {
        using var compiler = new ShaderCompiler(options);
        if (!compiler.TryCompile(inputPath, out var compiledShader))
        {
            Console.WriteLine($"Skipping '{inputPath}', no entry point found");
            return 0;
        }

        ShaderPackageWriter.WriteToFile(compiledShader!, outputPath);
        Console.WriteLine(
            $"Compiled '{inputPath}' -> '{outputPath}' ({compiledShader!.Kind}, {compiledShader.Stages.Length} stage(s))");
        return 0;
    }
    catch (SlangCompileException ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }
}

static int RunDiscover(string[] args)
{
    string? root = null;
    List<string> prefixes = [];

    for (var i = 1; i < args.Length; i++)
    {
        var arg = args[i];
        switch (arg)
        {
            case "--prefix":
                prefixes.Add(args[++i]);
                break;
            default:
                if (root != null)
                {
                    Console.Error.WriteLine($"Unexpected argument '{arg}'");
                    return 1;
                }

                root = arg;
                break;
        }
    }

    if (root == null)
    {
        Console.Error.WriteLine("Usage: rin-slang discover <searchRoot> [--prefix <prefix>]...");
        return 1;
    }

    var found = ShaderReferenceScanner.ScanDirectory(root);
    var filtered = prefixes.Count == 0
        ? found
        : found.Where(key => prefixes.Any(p => key.StartsWith(p, StringComparison.Ordinal)));

    foreach (var key in filtered.Order(StringComparer.Ordinal))
        Console.WriteLine(key);

    return 0;
}

static int RunCompileReferenced(string[] args)
{
    string? prefix = null;
    string? repoRoot = null;
    string? outputDir = null;
    List<string> scanPaths = [];
    var options = new ShaderCompilerOptions();

    for (var i = 1; i < args.Length; i++)
    {
        var arg = args[i];
        switch (arg)
        {
            case "--prefix":
                prefix = args[++i];
                break;
            case "--repo-root":
                repoRoot = args[++i];
                break;
            case "--output":
                outputDir = args[++i];
                break;
            case "--scan":
                scanPaths.Add(args[++i]);
                break;
            case "-D":
            case "--define":
            {
                if (!TrySplitPair(args[++i], out var name, out var value))
                {
                    Console.Error.WriteLine($"Invalid -D value '{args[i]}', expected NAME=VALUE");
                    return 1;
                }

                options.AddDefine(name, value);
                break;
            }
            default:
                Console.Error.WriteLine($"Unexpected argument '{arg}'");
                return 1;
        }
    }

    if (prefix == null || repoRoot == null || outputDir == null)
    {
        Console.Error.WriteLine(
            "Usage: rin-slang compile-referenced --prefix <prefix> --repo-root <path> --output <dir> [--scan <path>]... [-D <NAME>=<VALUE>]...");
        return 1;
    }

    if (scanPaths.Count == 0) scanPaths.Add(repoRoot);

    options.AddSearchPath(repoRoot);

    var referenced = scanPaths
        .SelectMany(ShaderReferenceScanner.ScanDirectory)
        .Where(key => key.StartsWith(prefix, StringComparison.Ordinal))
        .Distinct()
        .Order(StringComparer.Ordinal)
        .ToArray();

    if (referenced.Length == 0)
    {
        Console.WriteLine($"No shaders referenced under prefix '{prefix}'");
        return 0;
    }

    using var compiler = new ShaderCompiler(options);
    var hadError = false;

    foreach (var key in referenced)
    {
        var relativeKey = key[prefix.Length..].TrimStart('/');
        var sourcePath = Path.Combine(repoRoot, key.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(sourcePath))
        {
            Console.Error.WriteLine($"Referenced shader '{key}' does not exist at '{sourcePath}'");
            hadError = true;
            continue;
        }

        try
        {
            if (!compiler.TryCompile(sourcePath, out var compiledShader))
            {
                Console.WriteLine($"Skipping '{key}', no entry point found");
                continue;
            }

            var outputPath = Path.Combine(outputDir,
                Path.ChangeExtension(relativeKey, ".crsh").Replace('/', Path.DirectorySeparatorChar));

            ShaderPackageWriter.WriteToFile(compiledShader!, outputPath);
            Console.WriteLine(
                $"Compiled '{key}' -> '{outputPath}' ({compiledShader!.Kind}, {compiledShader.Stages.Length} stage(s))");
        }
        catch (SlangCompileException ex)
        {
            Console.Error.WriteLine($"Failed to compile '{key}': {ex.Message}");
            hadError = true;
        }
    }

    return hadError ? 1 : 0;
}

static void PrintUsage()
{
    Console.Error.WriteLine("""
                             Usage:
                               rin-slang compile <input.slang> [-o <output.crsh>] [-I <searchPath>]... [-D <NAME>=<VALUE>]... [-A <ALIAS>=<path>]...
                               rin-slang discover <searchRoot> [--prefix <prefix>]...
                               rin-slang compile-referenced --prefix <prefix> --repo-root <path> --output <dir> [--scan <path>]... [-D <NAME>=<VALUE>]...
                             """);
}

static bool TrySplitPair(string value, out string key, out string val)
{
    var eq = value.IndexOf('=');
    if (eq < 0)
    {
        key = string.Empty;
        val = string.Empty;
        return false;
    }

    key = value[..eq];
    val = value[(eq + 1)..];
    return true;
}
