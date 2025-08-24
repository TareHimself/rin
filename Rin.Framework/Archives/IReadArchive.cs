using Rin.Framework.Extensions;

namespace Rin.Framework.Archives;

public interface IReadArchive : IArchive
{
    public IEnumerable<string> Keys { get; }
    public int Count { get; }

    public Stream CreateReadStream(string key);

    public byte[] Read(string key)
    {
        using var stream = CreateReadStream(key);
        return stream.ReadAll();
    }

    public async Task<byte[]> ReadAsync(string key)
    {
        await using var stream = CreateReadStream(key);
        return await stream.ReadAllAsync();
    }
}