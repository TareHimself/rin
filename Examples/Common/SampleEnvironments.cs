using Rin.Core.Graphics;

namespace Examples.Common;

public static class SampleEnvironments
{
    private const string BaseUrl =
        "https://media.githubusercontent.com/media/KhronosGroup/glTF-Sample-Environments/main/";

    // The largest sample is about 80 MB.
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(5) };

    public static string CacheDirectory { get; } = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Rin", "Examples", "hdris");

    public static async Task<byte[]> Load(string name, CancellationToken cancellation = default)
    {
        var cached = Path.Join(CacheDirectory, name + ".hdr");
        if (File.Exists(cached)) return await File.ReadAllBytesAsync(cached, cancellation);

        var bytes = await Http.GetByteArrayAsync(BaseUrl + name + ".hdr", cancellation);
        if (!RadianceImage.IsRadiance(bytes))
            throw new InvalidDataException($"The download of '{name}' is not a Radiance image.");

        Directory.CreateDirectory(CacheDirectory);
        var partial = cached + ".part";
        await File.WriteAllBytesAsync(partial, bytes, cancellation);
        File.Move(partial, cached, true);
        return bytes;
    }
}
