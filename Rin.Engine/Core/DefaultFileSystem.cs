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

    private string ToNativePath(FileUri uri)
    {
        return uri.Scheme switch
        {
            "system" => OperatingSystem.IsLinux() ? "/" + Path.Combine(uri.Path) : Path.Combine(uri.Path),
            _ => throw new NotImplementedException()
        };
    }
    public bool Exists(FileUri uri)
    {
        return Path.Exists(ToNativePath(uri));
    }

    public Stream OpenRead(FileUri uri)
    {
        return File.OpenRead(ToNativePath(uri));
    }

    public Stream OpenWrite(FileUri uri)
    {
        return File.OpenWrite(ToNativePath(uri));
    }

    public string ReadAllText(FileUri uri)
    {
        return File.ReadAllText(ToNativePath(uri));
    }
}