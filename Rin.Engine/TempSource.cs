using System.Collections.Concurrent;
using Rin.Sources;
using Rin.Sources.Exceptions;

namespace Rin.Engine;

public class TempSource : ISource
{
    private readonly ConcurrentDictionary<string, Func<Stream>> _streams = [];
    public string BasePath { get; } = "temp";

    public Stream Read(string path)
    {
        if (_streams.TryGetValue(path, out var stream)) return stream();

        throw new FileNotFoundException();
    }

    public Stream Write(string path)
    {
        throw new WriteNotSupportedException();
    }

    public string AddStream(Func<Stream> stream)
    {
        var streamName = $"{BasePath}/{Guid.NewGuid().ToString().Replace("-", "")}";
        _streams.AddOrUpdate(streamName, stream, (_, _) => stream);
        return streamName;
    }
}