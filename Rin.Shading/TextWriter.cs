namespace Rin.Shading;

public class TextWriter(Stream stream) : IDisposable
{
    private readonly StreamWriter _target = new(stream);
    private string _tabs = string.Empty;


    public void Dispose()
    {
        _target.Flush();
        _target.Dispose();
    }

    public TextWriter AddTab()
    {
        _tabs += '\t';
        return this;
    }

    public TextWriter RemoveTab()
    {
        _tabs = _tabs[..^1];
        return this;
    }

    /// <summary>
    ///     Write a line of text to the output <see cref="Stream" />
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    public TextWriter Write(string line)
    {
        _target.WriteLine(_tabs + line);
        return this;
    }

    public TextWriter Flush()
    {
        _target.Flush();
        return this;
    }
}