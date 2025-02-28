namespace Rin.Engine.Core;

/// <summary>
/// Assumes all schemes and uri's are relative to the basePath
/// </summary>
/// <param name="basePath"></param>
public class DefaultFileSystem(string assetsBasePath) : IFileSystem
{
    // public bool Exists(string path)
    // {
    //     return Path.Exists(path);
    // }
    //
    // public Stream OpenRead(string path)
    // {
    //     return File.OpenRead(path);
    // }
    //
    // public Stream OpenWrite(string path)
    // {
    //     return File.OpenWrite(path);
    // }
    //
    // public string ReadAllText(string path)
    // {
    //     return File.ReadAllText(path);
    // }
    public bool Exists(FileUri uri)
    {
        return uri.Scheme switch
        {
            "system" => Path.Exists(Path.Join(uri.Path)),
            _ => throw new NotImplementedException()
        };
    }

    public Stream OpenRead(FileUri uri)
    {
        return uri.Scheme switch
        {
            "system" => File.OpenRead(Path.Join(uri.Path)),
            _ => throw new NotImplementedException()
        };
    }

    public Stream OpenWrite(FileUri uri)
    {
        return uri.Scheme switch
        {
            "system" => File.OpenWrite(Path.Join(uri.Path)),
            _ => throw new NotImplementedException()
        };
    }

    public string ReadAllText(FileUri uri)
    {
        return uri.Scheme switch
        {
            "system" => File.ReadAllText(Path.Join(uri.Path)),
            _ => throw new NotImplementedException()
        };
    }
}