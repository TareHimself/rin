namespace rin.Framework.Core;

public class DefaultFileSystem : IFileSystem
{
    public bool Exists(string path)
    {
        return Path.Exists(path);
    }

    public Stream OpenRead(string path)
    {
        return File.OpenRead(path);
    }

    public Stream OpenWrite(string path)
    {
        return File.OpenWrite(path);
    }

    public string ReadAllText(string path)
    {
        return File.ReadAllText(path);
    }
}