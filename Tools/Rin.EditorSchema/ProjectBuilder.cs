using System.Diagnostics;

namespace Rin.EditorSchema;

/// <summary>
///     Builds a project with the dotnet CLI and finds the assembly it produced.
/// </summary>
internal static class ProjectBuilder
{
    public static bool Build(string project) =>
        RunDotnet(["build", project, "--nologo", "-v", "q", "-clp:ErrorsOnly"], echo: true).ExitCode == 0;

    public static string TargetPathOf(string project)
    {
        var (exitCode, output) = RunDotnet(["msbuild", project, "-getProperty:TargetPath"], echo: false);
        if (exitCode != 0) throw new InvalidOperationException($"Could not read the TargetPath of '{project}'.");
        return output.Trim();
    }

    private static (int ExitCode, string Output) RunDotnet(string[] arguments, bool echo)
    {
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = !echo };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var output = echo ? "" : process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output);
    }
}
