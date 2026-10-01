using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Rin.Slang;
using Rin.Slang.Compiler;
using Rin.Shade.Transpiler;

namespace Rin.Shade.MSBuild;

/// <summary>
///     Transpiles every [Shader("...")]-attributed class reachable from Sources, compiles the result
///     with the real Slang compiler, and reports EmbeddedResource-ready items back - all in one
///     in-process step, mirroring Rin.Slang.MSBuild's CompileRinSlangShaders but for C#-authored
///     shaders. No .slang file is ever written to the repo's tracked source tree: ShaderCompiler
///     still needs a real file to read (it resolves #include by reading lines off disk), but that
///     file only ever needs to exist under OutputRoot (an obj-relative scratch location) for as long
///     as the compile takes. DiscoverPrefix/OutputRoot/OutputSubpath/AssemblyName mirror
///     CompileRinSlangShaders' own parameters and LogicalName convention exactly, so the embedded
///     result resolves through Global.Sources/AssemblyContentResource the same way a hand-written
///     shader's compiled output does - just under whatever alias the consuming project registers for
///     its own Rin.Shade-owned prefix (a sibling of, not the same as, its Rin.Slang one - that's what
///     keeps the two pipelines from ever fighting over the same embedded resource name).
/// </summary>
public sealed class CompileRinShadeShaders : Microsoft.Build.Utilities.Task
{
    [Required] public string RepoRoot { get; set; } = "";

    [Required] public string DiscoverPrefix { get; set; } = "";

    [Required] public ITaskItem[] Sources { get; set; } = [];

    [Required] public ITaskItem[] References { get; set; } = [];

    [Required] public string OutputRoot { get; set; } = "";

    [Required] public string OutputSubpath { get; set; } = "";

    [Required] public string AssemblyName { get; set; } = "";

    /// <summary>
    ///     When set, every transpiled shader's Slang is also written here (same relative path as its
    ///     [Shader] path), so the exact text that was compiled can be read and diffed. Debugging aid only,
    ///     nothing reads it back.
    /// </summary>
    public string? GeneratedDirectory { get; set; }

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

                // Scratch-only: ShaderCompiler resolves #include by reading lines off a real file, so
                // one has to exist somewhere - never the tracked Shaders/ tree, since nothing else
                // ever needs to see this file, unlike a hand-written .slang source.
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

    // ShadeEmitResult keys by class name only; [Shader("...")]'s path argument is only ever read
    // here, task-local - a pure syntax walk (mirrors ShaderReferenceScanner's own attribute
    // matching), since the class's simple name is all ShadeEmitter's own dictionary key needs.
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
