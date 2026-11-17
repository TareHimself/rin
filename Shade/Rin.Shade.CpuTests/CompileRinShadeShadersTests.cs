using System.Collections;
using System.Runtime.CompilerServices;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Rin.Shade.MSBuild;
using Rin.Shade.Tests;

namespace Rin.Shade.CpuTests;

/// <summary>
/// Runs the build task itself with the real Slang compiler, against shader files in a temp folder.
/// </summary>
public class CompileRinShadeShadersTests
{
    private string _directory = null!;
    private RecordingBuildEngine _engine = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Directory.CreateTempSubdirectory("rin-shade-task").FullName;
        _engine = new RecordingBuildEngine();
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_directory, true);
    }

    private static string RepoRoot([CallerFilePath] string callerPath = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(callerPath)!, "..", ".."));

    private static string Shader(string body) => $$"""
        using Rin.Shade;

        namespace TaskCheck;

        public struct TaskPush
        {
            public float Value;
        }

        public static class TaskIntrinsics
        {
            [SlangExpression("noSuchSlangFunction(@0)")]
            public static extern float Broken(float value);
        }

        [Shader]
        public class TaskShader : Shader
        {
            [Push] protected TaskPush Push;

            [Compute(1, 1, 1)]
            public void Compute()
            {
                {{body}}
            }
        }
        """;

    private CompileRinShadeShaders TaskFor(string source)
    {
        var sourcePath = Path.Combine(_directory, "TaskShader.cs");
        File.WriteAllText(sourcePath, source);
        return new CompileRinShadeShaders
        {
            BuildEngine = _engine,
            RepoRoot = RepoRoot(),
            Sources = [new TaskItem(sourcePath)],
            References = CompilationBuilder.References.Select(r => (ITaskItem)new TaskItem(r.Display!)).ToArray(),
            OutputRoot = Path.Combine(_directory, "out")
        };
    }

    [Test]
    public void AShaderIsCompiledToAPackageNamedAfterItsFullName()
    {
        var task = TaskFor(Shader("var doubled = Push.Value * 2f;"));

        var succeeded = task.Execute();

        Assert.That(_engine.Errors, Is.Empty);
        Assert.That(succeeded, Is.True);
        var package = task.CompiledFiles.Single();
        Assert.That(package.GetMetadata("LogicalName"), Is.EqualTo("TaskCheck.TaskShader.crsh"));
        Assert.That(File.Exists(package.ItemSpec), Is.True);
    }

    [Test]
    public void AnUnchangedShaderIsReusedOnTheNextRun()
    {
        var source = Shader("var doubled = Push.Value * 2f;");
        TaskFor(source).Execute();
        _engine.Messages.Clear();

        var succeeded = TaskFor(source).Execute();

        Assert.That(succeeded, Is.True);
        Assert.That(_engine.Messages, Has.Some.StartsWith("Up to date 'TaskShader'"));
    }

    [Test]
    public void ARejectedConstructFailsBeforeAnythingIsCompiled()
    {
        var task = TaskFor(Shader("try { } catch { }"));

        var succeeded = task.Execute();

        Assert.That(succeeded, Is.False);
        Assert.That(_engine.Errors.Select(e => e.Message), Has.Some.Contains("SHADE0003"));
        Assert.That(Directory.Exists(Path.Combine(_directory, "out")), Is.False);
    }

    [Test]
    public void ASlangErrorIsReportedAgainstTheCSharpLineItCameFrom()
    {
        var source = Shader("var broken = TaskIntrinsics.Broken(Push.Value);");
        var brokenLine = source.Split('\n').TakeWhile(line => !line.Contains("var broken")).Count() + 1;
        var task = TaskFor(source);

        var succeeded = task.Execute();

        Assert.That(succeeded, Is.False);
        var error = _engine.Errors.Single();
        Assert.That(error.Code, Is.EqualTo("SLANG30015"));
        Assert.That(error.Message, Does.Contain("noSuchSlangFunction").And.Contain("TaskCheck.TaskShader"));
        Assert.That(error.File, Is.EqualTo(task.Sources[0].ItemSpec));
        Assert.That(error.LineNumber, Is.EqualTo(brokenLine));
    }

    private sealed class RecordingBuildEngine : IBuildEngine
    {
        public List<BuildErrorEventArgs> Errors { get; } = [];
        public List<BuildWarningEventArgs> Warnings { get; } = [];
        public List<string> Messages { get; } = [];

        public bool ContinueOnError => false;
        public int LineNumberOfTaskNode => 0;
        public int ColumnNumberOfTaskNode => 0;
        public string ProjectFileOfTaskNode => "";

        public void LogErrorEvent(BuildErrorEventArgs e) => Errors.Add(e);
        public void LogWarningEvent(BuildWarningEventArgs e) => Warnings.Add(e);
        public void LogMessageEvent(BuildMessageEventArgs e) => Messages.Add(e.Message ?? "");
        public void LogCustomEvent(CustomBuildEventArgs e) { }

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties,
            IDictionary targetOutputs) => throw new NotSupportedException();
    }
}
