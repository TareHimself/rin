using System.Collections.Concurrent;
using Rin.Framework.Sources;
using Rin.Framework.Sources.Exceptions;

namespace Rin.Framework;

public class TemporarySource : ISource
{
    private readonly ConcurrentDictionary<string, Func<Stream>> _streams = [];
    public string BasePath { get; } = "Temporary";

    public Stream Read(string path)
    {
        if (_streams.TryGetValue(path, out var stream)) return stream();

        throw new FileNotFoundException();
    }

    public Stream Write(string path)
    {
        throw new WriteNotSupportedException();
    }

    public string CreateReadable(Func<Stream> stream)
    {
        var streamName = $"{BasePath}/{Guid.NewGuid().ToString().Replace("-", "")}";
        _streams.AddOrUpdate(streamName, stream, (_, _) => stream);
        return streamName;
    }
}