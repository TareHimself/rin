using System.Text.RegularExpressions;
using Rin.Core.Shared.Video;
using Rin.TestSupport;

namespace Rin.Core.Tests.Shared.Video;

public class WebmDemuxerTests
{
    [Test]
    public void VideoDemuxResultMirrorsVideoHpp()
    {
        var header = RepoFiles.NativeSource("Rin.Native", "video.hpp");
        var body = Regex.Match(header, @"enum VideoDemuxResult[^{]*\{([^}]*)\}").Groups[1].Value;
        var expected = body.Split(',')
            .Select(entry => Regex.Replace(entry, "//.*", string.Empty).Trim())
            .Where(entry => entry.Length > 0)
            .Select(entry => (Name: entry.Split('=')[0].Trim(), Value: int.Parse(entry.Split('=')[1].Trim())))
            .OrderBy(entry => entry.Value)
            .ToArray();

        var actual = Enum.GetValues<VideoDemuxResult>()
            .Select(value => (Name: "VIDEO_DEMUX_" + ToUpperSnake(value.ToString()), Value: (int)value))
            .OrderBy(entry => entry.Value)
            .ToArray();

        Assert.That(actual, Is.EqualTo(expected));
    }

    private static string ToUpperSnake(string pascal)
    {
        return Regex.Replace(pascal, "(?<=[a-z])(?=[A-Z])", "_").ToUpperInvariant();
    }
}
