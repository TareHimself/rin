using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Rin.Slang.Compiler;
using Rin.Slang.Discovery;

namespace Rin.Slang.MSBuild;

/// <summary>
///     Compiles .slang shaders to .crsh and reports them back as EmbeddedResource-ready items (with a
///     correctly-set LogicalName), in one in-process step. Replaces an earlier attempt built out of
///     &lt;Exec&gt; + hand-rolled MSBuild item/property XML - that approach kept hitting real MSBuild
///     footguns (unqualified %() metadata batching across unrelated items, %() not resolving inside
///     property functions, Windows CLI trailing-backslash quoting) that a real C# implementation just
///     doesn't have.
///
///     Two modes:
///     - Library (set DiscoverPrefix): scans the whole repo for [GraphicsShader]/[ComputeShader]/
///       MakeGraphics/MakeCompute references under that prefix, and compiles only those - unused/broken/
///       WIP files under the shared Shaders/ tree never block the build.
///     - Leaf (set Sources instead): compiles exactly the given files.
/// </summary>
public sealed class CompileRinSlangShaders : Microsoft.Build.Utilities.Task
{
    [Required] public string RepoRoot { get; set; } = "";

    public string? DiscoverPrefix { get; set; }

    public ITaskItem[]? Sources { get; set; }

    [Required] public string OutputRoot { get; set; } = "";

    [Required] public string OutputSubpath { get; set; } = "";

    [Required] public string AssemblyName { get; set; } = "";

    [Output] public ITaskItem[] CompiledFiles { get; set; } = [];

    public override bool Execute()
    {
        var toCompile = new List<(string relativeOutput, string sourcePath)>();

        if (!string.IsNullOrEmpty(DiscoverPrefix))
        {
            var referenced = ShaderReferenceScanner.ScanDirectory(RepoRoot)
                .Where(key => key.StartsWith(DiscoverPrefix, StringComparison.Ordinal))
                .OrderBy(key => key, StringComparer.Ordinal);

            foreach (var key in referenced)
            {
                var sourcePath = Path.Combine(RepoRoot, key.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(sourcePath))
                {
                    Log.LogError($"Referenced shader '{key}' does not exist at '{sourcePath}'");
                    continue;
                }

                toCompile.Add((key[DiscoverPrefix.Length..].TrimStart('/'), sourcePath));
            }
        }
        else if (Sources != null)
        {
            foreach (var item in Sources)
            {
                var sourcePath = item.GetMetadata("FullPath");
                var relativeOutput = item.GetMetadata("RecursiveDir") + item.GetMetadata("Filename") +
                                      item.GetMetadata("Extension");
                toCompile.Add((relativeOutput, sourcePath));
            }
        }

        var options = new ShaderCompilerOptions();
        options.AddSearchPath(RepoRoot);

        var outputs = new List<ITaskItem>();

        try
        {
            using var compiler = new ShaderCompiler(options);

            foreach (var (relativeOutput, sourcePath) in toCompile)
                try
                {
                    if (!compiler.TryCompile(sourcePath, out var compiledShader))
                    {
                        Log.LogMessage(MessageImportance.Low, $"Skipping '{sourcePath}', no entry point found");
                        continue;
                    }

                    var outputRelative = Path.ChangeExtension(relativeOutput, ".crsh");
                    var outputPath = Path.Combine(OutputRoot, outputRelative);
                    ShaderPackageWriter.WriteToFile(compiledShader!, outputPath);

                    var logicalName = AssemblyName + "." +
                                       (OutputSubpath + outputRelative).Replace('\\', '.').Replace('/', '.');

                    var outputItem = new TaskItem(outputPath);
                    outputItem.SetMetadata("LogicalName", logicalName);
                    outputs.Add(outputItem);

                    Log.LogMessage(MessageImportance.Normal, $"Compiled '{sourcePath}' -> '{outputPath}'");
                }
                catch (SlangCompileException ex)
                {
                    Log.LogError($"Failed to compile '{sourcePath}': {ex.Message}");
                }
        }
        catch (SlangCompileException ex)
        {
            Log.LogError(ex.Message);
            return false;
        }

        CompiledFiles = [.. outputs];
        return !Log.HasLoggedErrors;
    }
}
