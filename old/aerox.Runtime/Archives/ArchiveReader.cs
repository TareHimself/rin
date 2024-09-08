using aerox.Runtime.Extensions;

namespace aerox.Runtime.Archives;

public abstract class ArchiveReader : Archive
{
    public abstract IEnumerable<string> Keys { get; }
    public abstract int Count { get; }
    
    public abstract Stream CreateReadStream(string key);
    
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