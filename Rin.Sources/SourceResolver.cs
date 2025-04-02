using Rin.Sources.Exceptions;

namespace Rin.Sources;

public class SourceResolver : ISource
{
    private readonly Dictionary<string, ISource> _sources = [];

    public ISource[] Sources
    {
        init
        {
            foreach (var source in value) AddSource(source);
        }
    }

    public string BasePath => "";

    public Stream Read(string path)
    {
        return GetSourceFor(path).Read(path);
    }

    public Stream Write(string path)
    {
        return GetSourceFor(path).Write(path);
    }

    public SourceResolver AddSource(ISource source)
    {
        _sources.Add(source.BasePath, source);
        return this;
    }

    private ISource GetSourceFor(string path)
    {
        var key = _sources.Keys.Where(path.StartsWith).MaxBy(c => c.Length) ?? throw new NoValidSourceException();
        return _sources[key];
    }
}