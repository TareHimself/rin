using System.Text.RegularExpressions;
using Rin.TestSupport;

namespace Rin.Audio.Miniaudio.Tests;

public class MiniaudioEngineTests
{
    [Test]
    public void AudioResultMirrorsResultHpp()
    {
        var header = RepoFiles.NativeSource("Rin.Audio.Miniaudio.Native", "result.hpp");
        var body = Regex.Match(header, @"enum class AudioResult[^{]*\{([^}]*)\}").Groups[1].Value;
        var expected = body.Split(',')
            .Select(entry => Regex.Replace(entry, "//.*", string.Empty).Split('=')[0].Trim())
            .Where(name => name.Length > 0)
            .ToArray();

        Assert.That(Enum.GetNames<MiniaudioEngine.AudioResult>(), Is.EqualTo(expected));
    }
}
