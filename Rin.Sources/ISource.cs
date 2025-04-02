namespace Rin.Sources;

public interface ISource
{
    /// <summary>
    ///     Used for resolution
    /// </summary>
    public string BasePath { get; }

    public Stream Read(string path);

    public Stream Write(string path);
}