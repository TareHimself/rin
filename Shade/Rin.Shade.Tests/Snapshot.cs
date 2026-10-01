using System.Runtime.CompilerServices;

namespace Rin.Shade.Tests;

/// <summary>
/// Compares emitted Slang against a golden file in Snapshots/ next to the test. A mismatch writes the
/// actual output to a .received.slang file beside it; run the tests with RIN_SHADE_SNAPSHOTS=update to
/// accept the new output into the golden files, then review the change with git diff.
/// </summary>
internal static class Snapshot
{
    private const string UpdateVariable = "RIN_SHADE_SNAPSHOTS";

    public static void Verify(string actual, string? suffix = null, [CallerFilePath] string callerFile = "",
        [CallerMemberName] string testName = "")
    {
        var directory = Path.Combine(Path.GetDirectoryName(callerFile)!, "Snapshots");
        var name = $"{Path.GetFileNameWithoutExtension(callerFile)}.{testName}{(suffix is null ? "" : "." + suffix)}";
        var approvedPath = Path.Combine(directory, $"{name}.slang");
        var receivedPath = Path.Combine(directory, $"{name}.received.slang");
        var normalized = Normalize(actual);

        if (Environment.GetEnvironmentVariable(UpdateVariable) == "update")
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(approvedPath, normalized);
            if (File.Exists(receivedPath)) File.Delete(receivedPath);
            return;
        }

        if (File.Exists(approvedPath) && Normalize(File.ReadAllText(approvedPath)) == normalized)
        {
            if (File.Exists(receivedPath)) File.Delete(receivedPath);
            return;
        }

        Directory.CreateDirectory(directory);
        File.WriteAllText(receivedPath, normalized);
        Assert.Fail(File.Exists(approvedPath)
            ? $"Output differs from {name}.slang. {FirstDifference(File.ReadAllText(approvedPath), normalized)}\n" +
              $"Received output: {receivedPath}\nAccept it by running the tests with {UpdateVariable}=update."
            : $"No snapshot {name}.slang yet. Received output: {receivedPath}\n" +
              $"Create it by running the tests with {UpdateVariable}=update.");
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n");

    private static string FirstDifference(string approved, string actual)
    {
        var expectedLines = Normalize(approved).Split('\n');
        var actualLines = actual.Split('\n');
        for (var i = 0; i < Math.Max(expectedLines.Length, actualLines.Length); i++)
        {
            var expectedLine = i < expectedLines.Length ? expectedLines[i] : "<end>";
            var actualLine = i < actualLines.Length ? actualLines[i] : "<end>";
            if (expectedLine != actualLine)
                return $"First difference at line {i + 1}:\n  expected: {expectedLine}\n  actual:   {actualLine}";
        }

        return "";
    }
}
