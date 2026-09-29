using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Rin.Shade.Transpiler;

namespace Rin.Shade.MSBuild;

/// <summary>
///     Transpiles every [Shader("...")]-attributed class reachable from Sources to real Slang source
///     text, written directly into the repo's tracked source tree at the path each attribute names -
///     not to an intermediate/obj directory, since Rin.Slang.Discovery resolves a shader's path
///     against RepoRoot, the same as a hand-written .slang file. Deliberately has no MSBuild [Output]
///     items: the only contract with Rin.Slang.MSBuild is "a real file exists on disk in time", not a
///     shared item list - this task runs, then Rin.Slang.MSBuild's own discovery/compile step runs
///     completely unaware Rin.Shade was ever involved.
/// </summary>
public sealed class CompileRinShadeShaders : Microsoft.Build.Utilities.Task
{
    [Required] public string RepoRoot { get; set; } = "";

    [Required] public ITaskItem[] Sources { get; set; } = [];

    [Required] public ITaskItem[] References { get; set; } = [];

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

        foreach (var (className, shaderPath) in CollectShaderPaths(scratch.LocalTrees))
        {
            if (!result.Shaders.TryGetValue(className, out var slang)) continue;

            var outputPath = Path.Combine(RepoRoot, shaderPath.Replace('/', Path.DirectorySeparatorChar));

            // A repo-tracked/generated file that already matches skips the write - avoids marking it
            // dirty (touching mtime, tripping incremental-build/git-status noise) on every build when
            // nothing actually changed.
            if (File.Exists(outputPath) && File.ReadAllText(outputPath) == slang) continue;

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllText(outputPath, slang);
            Log.LogMessage(MessageImportance.High, $"Transpiled '{className}' -> '{outputPath}'");
        }

        return !Log.HasLoggedErrors;
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
