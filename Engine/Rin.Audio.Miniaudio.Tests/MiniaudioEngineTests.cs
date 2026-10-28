using System.Text.RegularExpressions;

namespace Rin.Audio.Miniaudio.Tests;

public class MiniaudioEngineTests
{
    [Test]
    public void AudioResult_MirrorsResultHpp()
    {
        var header = File.ReadAllText(Path.Combine(FindRepoRoot(), "native", "Rin.Audio.Miniaudio.Native", "src",
            "result.hpp"));
        var body = Regex.Match(header, @"enum class AudioResult[^{]*\{([^}]*)\}").Groups[1].Value;
        var expected = body.Split(',')
            .Select(entry => Regex.Replace(entry, "//.*", string.Empty).Split('=')[0].Trim())
            .Where(name => name.Length > 0)
            .ToArray();

        Assert.That(Enum.GetNames<MiniaudioEngine.AudioResult>(), Is.EqualTo(expected));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "rin.sln")))
            dir = dir.Parent;
        if (dir is null)
            throw new InvalidOperationException($"Could not find rin.sln above {AppContext.BaseDirectory}");
        return dir.FullName;
    }
}
