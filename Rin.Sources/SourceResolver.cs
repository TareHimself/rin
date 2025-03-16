using Rin.Sources.Exceptions;

namespace Rin.Sources;

public class SourceResolver : ISource
{

    public ISource[] Sources
    {
        init
        {
            foreach (var source in value)
            {
                AddSource(source);
            }
        }
    }

    private Dictionary<string, ISource> _sources = [];
    
    public string BasePath => "/";

    public SourceResolver AddSource(ISource source)
    {
        _sources.Add(source.BasePath,source);
        return this;
    }

    private ISource GetSourceFor(string path)
    {
        var key = _sources.Keys.Where(path.StartsWith).MaxBy(c => c.Length) ?? throw new NoValidSourceException();
        return _sources[key];
    }
    public Stream Read(string path)
    {
        return GetSourceFor(path).Read(path);
    }

    public Stream Write(string path)
    {
        return GetSourceFor(path).Write(path);
    }
}