using System.Diagnostics;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Rin.Slang;
using Rin.Slang.Compiler;
using Rin.Shade.Transpiler;

namespace Rin.Shade.MSBuild;

/// <summary>
/// Transpiles every <c>[Shader]</c> class in <see cref="Sources" />, compiles the Slang of each shader on its
/// own thread with the real Slang compiler, and returns the results as embedded-resource items named after the
/// class's full name plus <c>.crsh</c>. Slang errors are reported against the C# line they were lowered from.
/// The intermediate .slang file is written only under <see cref="OutputRoot" />, never into the tracked source tree.
/// </summary>
public sealed class CompileRinShadeShaders : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// Repository root, used as the Slang search path and portable root.
    /// </summary>
    [Required] public string RepoRoot { get; set; } = "";

    /// <summary>
    /// C# source files to scan for shaders.
    /// </summary>
    [Required] public ITaskItem[] Sources { get; set; } = [];

    /// <summary>
    /// Assemblies the sources compile against.
    /// </summary>
    [Required] public ITaskItem[] References { get; set; } = [];

    /// <summary>
    /// Directory for the scratch .slang and compiled .crsh files, one pair per shader named by its full class name.
    /// </summary>
    [Required] public string OutputRoot { get; set; } = "";

    /// <summary>
    /// When set, the transpiled Slang of every shader is also written here as <c>&lt;full class name&gt;.slang</c>,
    /// for inspection. Nothing reads it back.
    /// </summary>
    public string? GeneratedDirectory { get; set; }

    /// <summary>
    /// How many shaders compile at once. 0 picks one thread per <see cref="ShadersPerThread" /> shaders, capped at
    /// the processor count, because every thread pays for its own Slang session; 1 compiles them one after another.
    /// </summary>
    public int MaxParallelism { get; set; }

    /// <summary>
    /// Shaders a thread must have to pay for creating its Slang session (about 450 ms, against under 10 ms per shader).
    /// </summary>
    public const int ShadersPerThread = 64;

    /// <summary>
    /// The compiled shaders as embedded-resource items.
    /// </summary>
    [Output] public ITaskItem[] CompiledFiles { get; set; } = [];

    public override bool Execute()
    {
        var localSources = Sources
            .Select(s => s.GetMetadata("FullPath"))
            .Select(path => new LocalSource(path, File.ReadAllText(path)))
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

        // An 'Invalid' operation means the C# did not bind, so the real cause is a C# error the compiler has not
        // reported yet, because this task runs before the compile.
        if (result.Diagnostics.Any(d => d.GetMessage().Contains("'Invalid'")))
            foreach (var error in scratch.Compilation.GetDiagnostics()
                         .Where(d => d.Severity == DiagnosticSeverity.Error && d.Location.SourceTree is { } tree &&
                                     scratch.LocalTrees.Contains(tree)))
                Log.LogError(error.ToString());

        if (Log.HasLoggedErrors) return false;

        var jobs = CollectJobs(scratch.Compilation, scratch.LocalTrees, result);
        foreach (var job in jobs) WriteSlang(job);

        var outcomes = CompileAll(jobs);
        var outputs = new List<ITaskItem>();
        foreach (var outcome in outcomes)
        {
            if (outcome.Item is not null)
            {
                outputs.Add(outcome.Item);
                Log.LogMessage(outcome.Reused ? MessageImportance.Normal : MessageImportance.High,
                    $"{(outcome.Reused ? "Up to date" : "Transpiled+compiled")} '{outcome.Job.ClassName}' -> '{outcome.Item.ItemSpec}'");
            }
            else
            {
                ReportFailure(outcome.Job, outcome.FailureText!);
            }
        }

        CompiledFiles = [.. outputs];
        return !Log.HasLoggedErrors;
    }

    private sealed record ShaderJob(string ClassName, string FullName, string Slang, SlangSourceMap SourceMap)
    {
        public string ResourceName => FullName + ".crsh";

        public string ScratchSlangPath(string outputRoot) => Path.Combine(outputRoot, FullName + ".slang");

        public string PackagePath(string outputRoot) => Path.Combine(outputRoot, ResourceName);
    }

    private sealed record CompileOutcome(ShaderJob Job, ITaskItem? Item, string? FailureText, bool Reused = false);

    private static List<ShaderJob> CollectJobs(Compilation compilation, IEnumerable<SyntaxTree> trees,
        ShadeEmitResult result)
    {
        var jobs = new List<ShaderJob>();
        foreach (var (className, fullName) in CollectShaderNames(compilation, trees))
        {
            if (!result.Shaders.TryGetValue(className, out var slang)) continue;
            jobs.Add(new ShaderJob(className, fullName, slang, result.SourceMaps[className]));
        }

        return jobs;
    }

    // ShaderCompiler resolves #include from a real file, so one must exist, but only as scratch under OutputRoot.
    private void WriteSlang(ShaderJob job)
    {
        Directory.CreateDirectory(OutputRoot);
        File.WriteAllText(job.ScratchSlangPath(OutputRoot), job.Slang);

        if (string.IsNullOrEmpty(GeneratedDirectory)) return;

        Directory.CreateDirectory(GeneratedDirectory);
        File.WriteAllText(Path.Combine(GeneratedDirectory, job.FullName + ".slang"), job.Slang);
    }

    /// <summary>
    /// Reuses the package of every shader whose Slang still hashes to what it was compiled from, and compiles the
    /// rest, spread over worker threads when there are enough of them. Each worker thread owns one
    /// <see cref="ShaderCompiler" />, because a Slang session must only be used from one thread at a time, and
    /// none is created when every shader is up to date.
    /// </summary>
    private CompileOutcome[] CompileAll(List<ShaderJob> jobs)
    {
        var options = new ShaderCompilerOptions();
        options.AddSearchPath(RepoRoot);
        options.SetPortableRoot(RepoRoot);

        var outcomes = new CompileOutcome[jobs.Count];
        var stale = new List<int>();
        for (var i = 0; i < jobs.Count; i++)
            if (ShaderCompiler.IsUpToDate(jobs[i].PackagePath(OutputRoot), jobs[i].ScratchSlangPath(OutputRoot), options))
                outcomes[i] = new CompileOutcome(jobs[i], OutputItem(jobs[i]), null, Reused: true);
            else
                stale.Add(i);

        var parallelism = MaxParallelism > 0 ? MaxParallelism : int.Max(1, stale.Count / ShadersPerThread);
        parallelism = int.Min(parallelism, int.Max(Environment.ProcessorCount, 1));
        var stopwatch = Stopwatch.StartNew();

        using var compilers = new ThreadLocal<ShaderCompiler>(() => new ShaderCompiler(options), trackAllValues: true);

        try
        {
            Parallel.ForEach(stale, new ParallelOptions { MaxDegreeOfParallelism = parallelism },
                i => outcomes[i] = Compile(compilers.Value!, jobs[i]));
        }
        finally
        {
            foreach (var compiler in compilers.Values) compiler.Dispose();
        }

        Log.LogMessage(MessageImportance.Normal,
            $"Compiled {stale.Count} of {jobs.Count} shaders in {stopwatch.ElapsedMilliseconds} ms on up to {parallelism} threads, {jobs.Count - stale.Count} up to date");
        return outcomes;
    }

    private ITaskItem OutputItem(ShaderJob job)
    {
        var outputItem = new TaskItem(job.PackagePath(OutputRoot));
        outputItem.SetMetadata("LogicalName", job.ResourceName);
        return outputItem;
    }

    private CompileOutcome Compile(ShaderCompiler compiler, ShaderJob job)
    {
        try
        {
            if (!compiler.TryCompile(job.ScratchSlangPath(OutputRoot), out var compiledShader))
                return new CompileOutcome(job, null, "The shader has no entry point");

            ShaderPackageWriter.WriteToFile(compiledShader!, job.PackagePath(OutputRoot));
            return new CompileOutcome(job, OutputItem(job), null);
        }
        catch (SlangCompileException ex)
        {
            return new CompileOutcome(job, null, ex.Message);
        }
    }

    /// <summary>
    /// Logs each Slang diagnostic against the C# line it was lowered from, or against the scratch .slang when
    /// the line has no C# origin. Output without any diagnostic line is logged as one error.
    /// </summary>
    private void ReportFailure(ShaderJob job, string failureText)
    {
        var diagnostics = SlangDiagnostic.Parse(failureText);
        if (diagnostics.Count == 0)
        {
            Log.LogError($"Failed to compile Rin.Shade shader '{job.FullName}': {failureText}");
            return;
        }

        Log.LogMessage(MessageImportance.Low, failureText);
        foreach (var diagnostic in diagnostics)
        {
            // Slang ends a failed compile with a line-less "compilation ceased", which adds nothing to the real errors.
            if (diagnostic.Line == 0 && diagnostics.Count > 1) continue;

            var message = $"{diagnostic.Message} (Rin.Shade shader '{job.FullName}', line {diagnostic.Line} of its Slang)";
            var code = "SLANG" + diagnostic.Code;
            var (file, line, column) = Locate(job, diagnostic);

            if (diagnostic.Severity == "warning")
                Log.LogWarning("Slang", code, null, file, line, column, 0, 0, message);
            else if (diagnostic.Severity == "note")
                Log.LogMessage("Slang", code, null, file, line, column, 0, 0, MessageImportance.Normal, message);
            else
                Log.LogError("Slang", code, null, file, line, column, 0, 0, message);
        }
    }

    private (string File, int Line, int Column) Locate(ShaderJob job, SlangDiagnostic diagnostic)
    {
        if (job.SourceMap.Origin(diagnostic.Line) is { } origin)
        {
            var span = origin.GetLineSpan();
            return (span.Path, span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1);
        }

        return (job.ScratchSlangPath(OutputRoot), diagnostic.Line, diagnostic.Column);
    }

    // ShadeEmitResult is keyed by class name only, so the full name is looked up here.
    private static IEnumerable<(string ClassName, string FullName)> CollectShaderNames(Compilation compilation,
        IEnumerable<SyntaxTree> trees)
    {
        foreach (var tree in trees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var classDeclaration in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (model.GetDeclaredSymbol(classDeclaration) is not { } symbol) continue;
                if (!symbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "Rin.Shade.ShaderAttribute"))
                    continue;

                yield return (symbol.Name, symbol.ToDisplayString());
            }
        }
    }
}
