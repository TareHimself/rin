using rin.Framework.Core.Extensions;

namespace rin.Framework.Core.Archives;

public interface IArchiveReader : IArchive
{
    public IEnumerable<string> Keys { get; }
    public int Count { get; }
    
    public Stream CreateReadStream(string key);
    
    public virtual byte[] Read(string key)
    {
        using var stream = CreateReadStream(key);
        return stream.ReadAll();
    }
    
    public virtual async Task<byte[]> ReadAsync(string key)
    {
        await using var stream = CreateReadStream(key);
        return await stream.ReadAllAsync();
    }
}