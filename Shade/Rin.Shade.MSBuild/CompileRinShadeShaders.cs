using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Rin.Slang;
using Rin.Slang.Compiler;
using Rin.Shade.Transpiler;

namespace Rin.Shade.MSBuild;

/// <summary>
/// Transpiles every <c>[Shader("...")]</c> class in <see cref="Sources" />, compiles the Slang with
/// the real Slang compiler, and returns the results as embedded-resource items. The intermediate
/// .slang file is written only under <see cref="OutputRoot" />, never into the tracked source tree.
/// </summary>
public sealed class CompileRinShadeShaders : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// Repository root, used as the Slang search path and portable root.
    /// </summary>
    [Required] public string RepoRoot { get; set; } = "";

    /// <summary>
    /// Only shaders whose <c>[Shader]</c> path starts with this prefix are compiled.
    /// </summary>
    [Required] public string DiscoverPrefix { get; set; } = "";

    /// <summary>
    /// C# source files to scan for shaders.
    /// </summary>
    [Required] public ITaskItem[] Sources { get; set; } = [];

    /// <summary>
    /// Assemblies the sources compile against.
    /// </summary>
    [Required] public ITaskItem[] References { get; set; } = [];

    /// <summary>
    /// Directory for the scratch .slang and compiled .crsh files.
    /// </summary>
    [Required] public string OutputRoot { get; set; } = "";

    /// <summary>
    /// Path segment inserted into each embedded resource logical name.
    /// </summary>
    [Required] public string OutputSubpath { get; set; } = "";

    /// <summary>
    /// Name of the consuming assembly, the prefix of each logical name.
    /// </summary>
    [Required] public string AssemblyName { get; set; } = "";

    /// <summary>
    /// When set, the transpiled Slang of every shader is also written here, at its <c>[Shader]</c>
    /// path relative to <see cref="DiscoverPrefix" />, for inspection. Nothing reads it back.
    /// </summary>
    public string? GeneratedDirectory { get; set; }

    /// <summary>
    /// The compiled shaders as embedded-resource items.
    /// </summary>
    [Output] public ITaskItem[] CompiledFiles { get; set; } = [];

    public override bool Execute()
    {
        var localSources = Sources
            .Select(s => File.ReadAllText(s.GetMetadata("FullPath")))
            .ToList();

        var references = References
            .Select(r => (MetadataReference)MetadataReference.CreateFromFile(r.GetMetadata("FullPath")))
            .ToList();

        var scratch = ScratchCompilationBuilder.Build(localSources, references, "RinShadeMSBuildScratch");
        var result = ShadeEmitter.Emit(scratch.Compilation, scratch.LocalTrees);

        foreach (var diagnostic in result.Diagnostics)
            if (diagnostic.Severity == DiagnosticSeverity.Error)
                Log.LogError(diagnostic.ToString());
            else
                Log.LogWarning(diagnostic.ToString());

        if (Log.HasLoggedErrors) return false;

        var options = new ShaderCompilerOptions();
        options.AddSearchPath(RepoRoot);
        options.SetPortableRoot(RepoRoot);

        var outputs = new List<ITaskItem>();

        try
        {
            using var compiler = new ShaderCompiler(options);

            foreach (var (className, shaderPath) in CollectShaderPaths(scratch.LocalTrees))
            {
                if (!shaderPath.StartsWith(DiscoverPrefix, StringComparison.Ordinal)) continue;
                if (!result.Shaders.TryGetValue(className, out var slang)) continue;

                var relativeOutput = shaderPath[DiscoverPrefix.Length..].TrimStart('/');

                // ShaderCompiler resolves #include from a real file, so one must exist, but only as scratch under OutputRoot.
                var scratchSlangPath = Path.Combine(OutputRoot, relativeOutput);
                Directory.CreateDirectory(Path.GetDirectoryName(scratchSlangPath)!);
                File.WriteAllText(scratchSlangPath, slang);

                if (!string.IsNullOrEmpty(GeneratedDirectory))
                {
                    var generatedPath = Path.Combine(GeneratedDirectory, relativeOutput);
                    Directory.CreateDirectory(Path.GetDirectoryName(generatedPath)!);
                    File.WriteAllText(generatedPath, slang);
                }

                var outputPath = Path.Combine(OutputRoot, Path.ChangeExtension(relativeOutput, ".crsh"));

                try
                {
                    if (!compiler.TryCompile(scratchSlangPath, out var compiledShader))
                    {
                        Log.LogError($"Shader '{className}' ('{shaderPath}') has no entry point");
                        continue;
                    }

                    ShaderPackageWriter.WriteToFile(compiledShader!, outputPath);

                    var outputItem = new TaskItem(outputPath);
                    outputItem.SetMetadata("LogicalName", MakeLogicalName(relativeOutput));
                    outputs.Add(outputItem);

                    Log.LogMessage(MessageImportance.High, $"Transpiled+compiled '{className}' -> '{outputPath}'");
                }
                catch (SlangCompileException ex)
                {
                    Log.LogError($"Failed to compile Rin.Shade shader '{className}' ('{shaderPath}'): {ex.Message}");
                }
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

    private string MakeLogicalName(string relativeOutput)
    {
        var outputRelative = Path.ChangeExtension(relativeOutput, ".crsh");
        return AssemblyName + "." + (OutputSubpath + outputRelative).Replace('\\', '.').Replace('/', '.');
    }

    // ShadeEmitResult is keyed by class name only, so the [Shader] path is read here with a syntax walk.
    private static IEnumerable<(string ClassName, string Path)> CollectShaderPaths(IEnumerable<SyntaxTree> trees)
    {
        foreach (var tree in trees)
        foreach (var classDeclaration in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
        foreach (var attributeList in classDeclaration.AttributeLists)
        foreach (var attribute in attributeList.Attributes)
        {
            var name = attribute.Name.ToString();
            var simpleName = name[(name.LastIndexOf('.') + 1)..];
            if (simpleName is not ("Shader" or "ShaderAttribute")) continue;

            if (attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression is LiteralExpressionSyntax
                {
                    Token.Value: string path
                })
                yield return (classDeclaration.Identifier.Text, path);
        }
    }
}
