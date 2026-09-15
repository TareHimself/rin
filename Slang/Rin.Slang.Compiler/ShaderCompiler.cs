using System.Runtime.InteropServices;
using System.Text.Json;

namespace Rin.Slang.Compiler;

public sealed class ShaderCompiler : IDisposable
{
    private readonly Dictionary<string, string> _pathAliases;
    private readonly IReadOnlyList<string> _searchPaths;
    private readonly SlangSession _session;

    public ShaderCompiler(ShaderCompilerOptions options)
    {
        _searchPaths = options.SearchPaths.ToArray();
        _pathAliases = new Dictionary<string, string>(options.PathAliases);

        using var builder = new SlangSessionBuilder();
        builder.AddTargetSpirv();
        foreach (var searchPath in options.SearchPaths) builder.AddSearchPath(searchPath);
        foreach (var (name, value) in options.Defines) builder.AddPreprocessorDefinition(name, value);
        _session = builder.Build();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _session.Dispose();
    }

    public CompiledShader Compile(string sourcePath)
    {
        sourcePath = Path.GetFullPath(sourcePath);

        var included = new HashSet<string>();
        var joinedSource = string.Join('\n', ImportFile(sourcePath, included, _searchPaths, _pathAliases));

        using var loadDiagnostics = new SlangBlob();
        using var module = _session.LoadModuleFromSourceString(sourcePath, sourcePath, joinedSource,
            loadDiagnostics);
        if (module == null)
            throw new SlangCompileException("Failed to load slang shader module:\n" + loadDiagnostics.GetString());

        using var computeEntryPoint = module.FindEntryPointByName("compute");
        if (computeEntryPoint != null) return CompileCompute(module, computeEntryPoint);

        using var vertexEntryPoint = module.FindEntryPointByName("vertex");
        using var fragmentEntryPoint = module.FindEntryPointByName("fragment");
        if (vertexEntryPoint == null && fragmentEntryPoint == null)
            throw new NotAShaderException(
                "Shader has no 'compute' entry point, and no 'vertex'/'fragment' entry points");

        return CompileGraphics(module, vertexEntryPoint, fragmentEntryPoint);
    }

    /// <summary>
    ///     Like <see cref="Compile" />, but returns <see langword="false" /> instead of throwing when
    ///     <paramref name="sourcePath" /> has no entry point (i.e. it's an include-only library file, not a
    ///     shader). Real compile errors (syntax errors, unresolved includes, etc.) still throw.
    /// </summary>
    public bool TryCompile(string sourcePath, out CompiledShader? result)
    {
        try
        {
            result = Compile(sourcePath);
            return true;
        }
        catch (NotAShaderException)
        {
            result = null;
            return false;
        }
    }

    private CompiledShader CompileCompute(SlangModule module, SlangEntryPoint entryPoint)
    {
        var stage = CompileStage(module, entryPoint, "compute");
        var entryPointReflection = stage.Reflection.EntryPoints.FirstOrDefault() ??
                                    throw new SlangCompileException("Missing entry point reflection data");

        return new CompiledShader
        {
            Kind = ShaderKind.Compute,
            Stages = [stage],
            ThreadGroupSize = entryPointReflection.ThreadGroupSize
        };
    }

    private CompiledShader CompileGraphics(SlangModule module, SlangEntryPoint? vertexEntryPoint,
        SlangEntryPoint? fragmentEntryPoint)
    {
        List<CompiledStage> stages = [];
        if (vertexEntryPoint != null) stages.Add(CompileStage(module, vertexEntryPoint, "vertex"));
        if (fragmentEntryPoint != null) stages.Add(CompileStage(module, fragmentEntryPoint, "fragment"));

        return new CompiledShader
        {
            Kind = ShaderKind.Graphics,
            Stages = stages.ToArray()
        };
    }

    private CompiledStage CompileStage(SlangModule module, SlangEntryPoint entryPoint, string stageName)
    {
        using var composedProgram = _session.CreateComposedProgram(module, [entryPoint]) ??
                                     throw new SlangCompileException("Failed to create composed program.");

        using var linkedProgram = composedProgram.Link() ??
                                   throw new SlangCompileException("Failed to link composed program.");

        var codeDiagnostics = new SlangBlob();
        using var codeBlob = linkedProgram.GetEntryPointCode(0, 0, ref codeDiagnostics);
        if (codeBlob == null)
            throw new SlangCompileException("Failed to generate code.\n" + codeDiagnostics.GetString());

        using var reflectionBlob = linkedProgram.ToLayoutJson();
        var jsonString = Marshal.PtrToStringUTF8(reflectionBlob.GetDataPointer()) ??
                          throw new SlangCompileException("Failed to get reflection data.");

        var reflectionData = JsonSerializer.Deserialize(jsonString,
                                  SlangReflectionDataJsonContext.Default.SlangReflectionData) ??
                              throw new SlangCompileException("Failed to parse reflection data.");

        return new CompiledStage
        {
            Stage = stageName,
            Spirv = codeBlob.AsReadOnlySpan().ToArray(),
            Reflection = reflectionData
        };
    }

    private static bool TryResolveAlias(string includeKey, IReadOnlyDictionary<string, string> pathAliases,
        out string resolvedPath)
    {
        foreach (var (alias, target) in pathAliases)
        {
            if (includeKey == alias)
            {
                resolvedPath = Path.GetFullPath(target);
                return true;
            }

            var prefix = alias + "/";
            if (includeKey.StartsWith(prefix, StringComparison.Ordinal))
            {
                resolvedPath = Path.GetFullPath(Path.Combine(target, includeKey[prefix.Length..]));
                return true;
            }
        }

        resolvedPath = string.Empty;
        return false;
    }

    private static string ResolveInclude(string includeKey, string includingFileDirectory,
        IReadOnlyList<string> searchPaths, IReadOnlyDictionary<string, string> pathAliases)
    {
        if (TryResolveAlias(includeKey, pathAliases, out var aliasedPath))
        {
            if (File.Exists(aliasedPath)) return aliasedPath;
            throw new SlangCompileException(
                $"Path alias resolved #include \"{includeKey}\" to '{aliasedPath}', but that file does not exist");
        }

        var relativeToIncluder = Path.Combine(includingFileDirectory, includeKey);
        if (File.Exists(relativeToIncluder)) return Path.GetFullPath(relativeToIncluder);

        foreach (var searchPath in searchPaths)
        {
            var candidate = Path.Combine(searchPath, includeKey);
            if (File.Exists(candidate)) return Path.GetFullPath(candidate);
        }

        throw new SlangCompileException(
            $"Could not resolve #include \"{includeKey}\" (searched '{includingFileDirectory}' and {searchPaths.Count} search path(s))");
    }

    private static IEnumerable<string> ImportFile(string filePath, HashSet<string> included,
        IReadOnlyList<string> searchPaths, IReadOnlyDictionary<string, string> pathAliases)
    {
        var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        foreach (var line in File.ReadLines(filePath))
            if (line.StartsWith("#include"))
            {
                var includeKey = line[(line.IndexOf('"') + 1)..line.LastIndexOf('"')];
                if (included.Add(includeKey))
                {
                    var resolvedPath = ResolveInclude(includeKey, directory, searchPaths, pathAliases);
                    foreach (var importedLine in ImportFile(resolvedPath, included, searchPaths, pathAliases))
                        yield return importedLine;
                }
            }
            else
            {
                yield return line;
            }
    }
}
