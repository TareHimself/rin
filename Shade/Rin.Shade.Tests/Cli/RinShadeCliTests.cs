using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Rin.Shade.Tests.Cli;

/// <summary>
/// Runs the built rin-shade executable, so the argument handling, exit codes and files written are the real ones.
/// </summary>
public class RinShadeCliTests
{
    private const string Usings = """
        using Rin.Shade;

        namespace CliCheck;

        """;

    private const string PushStruct = """
        public struct DoublingPush
        {
            public float Value;
        }

        """;

    private static string ShaderClass(string name, string body = "var doubled = Push.Value * 2f;") => $$"""
        [Shader]
        public class {{name}} : Shader
        {
            [Push] protected DoublingPush Push;

            [Compute(1, 1, 1)]
            public void Compute()
            {
                {{body}}
            }
        }
        """;

    private static string DoublingShader(string body = "var doubled = Push.Value * 2f;") =>
        Usings + PushStruct + ShaderClass("DoublingShader", body);

    private string _directory = null!;

    [SetUp]
    public void CreateDirectory()
    {
        _directory = Directory.CreateTempSubdirectory("rin-shade-cli").FullName;
    }

    [TearDown]
    public void DeleteDirectory()
    {
        Directory.Delete(_directory, true);
    }

    private static string CliPath([CallerFilePath] string callerPath = "")
    {
        var configuration = typeof(RinShadeCliTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()
            ?.Configuration ?? "Debug";
        return Path.Combine(Path.GetDirectoryName(callerPath)!, "..", "..", "Rin.Shade.Cli", "bin", configuration,
            "net10.0", "rin-shade.dll");
    }

    private string WriteSource(string name, string source)
    {
        var path = Path.Combine(_directory, name);
        File.WriteAllText(path, source);
        return path;
    }

    private (int ExitCode, string Output, string Error) Run(params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = _directory
        };
        start.ArgumentList.Add(CliPath());
        foreach (var argument in arguments) start.ArgumentList.Add(argument);

        using var process = Process.Start(start)!;
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output, error.Result);
    }

    [Test]
    public void NoArgumentsPrintsTheUsageAndFails()
    {
        var (exitCode, _, error) = Run();

        Assert.That(exitCode, Is.EqualTo(1));
        Assert.That(error, Does.Contain("rin-shade compile <input.cs>"));
    }

    [Test]
    public void CompileWritesTheSlangNextToTheInputByDefault()
    {
        var input = WriteSource("Doubling.cs", DoublingShader());

        var (exitCode, output, _) = Run("compile", input);

        var slangPath = Path.Combine(_directory, "Doubling.slang");
        Assert.That(exitCode, Is.Zero);
        Assert.That(output, Does.Contain("Compiled 'DoublingShader'"));
        Assert.That(File.ReadAllText(slangPath), Does.Contain("var doubled = push.value * 2;"));
    }

    [Test]
    public void SeveralShadersAreWrittenAsOneFileEachNamedAfterTheClass()
    {
        var first = WriteSource("Doubling.cs", DoublingShader());
        var second = WriteSource("Tripling.cs", Usings + ShaderClass("TriplingShader"));
        var output = Path.Combine(_directory, "out", "shaders.slang");

        var (exitCode, _, _) = Run("compile", first, second, "-o", output);

        Assert.That(exitCode, Is.Zero);
        Assert.That(Directory.GetFiles(Path.Combine(_directory, "out")).Select(Path.GetFileName),
            Is.EquivalentTo(new[] { "shaders.DoublingShader.slang", "shaders.TriplingShader.slang" }));
    }

    [Test]
    public void AFileWithNoShaderFails()
    {
        var input = WriteSource("Plain.cs", "public class Plain { }");

        var (exitCode, _, error) = Run("compile", input);

        Assert.That(exitCode, Is.EqualTo(1));
        Assert.That(error, Does.Contain("No [Shader]-derived class found"));
    }

    [Test]
    public void ARejectedConstructFailsAndNamesTheFileAndLine()
    {
        var source = DoublingShader("try { } catch { }");
        var tryLine = source.Split('\n').TakeWhile(line => !line.Contains("try")).Count() + 1;
        var input = WriteSource("Rejected.cs", source);

        var (exitCode, _, error) = Run("compile", input);

        Assert.That(exitCode, Is.EqualTo(1));
        Assert.That(error, Does.Contain($"Rejected.cs({tryLine},").And.Contain("SHADE0003"));
    }
}
