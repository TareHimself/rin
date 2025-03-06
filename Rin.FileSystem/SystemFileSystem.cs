using JetBrains.Annotations;
using Rin.FileSystem.Exceptions;

namespace Rin.FileSystem;

public class SystemFileSystem : IFileSystem
{
    
    [PublicAPI]
    public static readonly string SystemScheme = "system";

    public string Target => "system";

    public bool Exists(FileUri uri)
    {
        if (uri.Target != Target) throw new UnsupportedFileUriException(this,uri);
        return File.Exists(Path.Join(uri.Path));
    }

    public Stream OpenRead(FileUri uri)
    {
        if (uri.Target != Target) throw new UnsupportedFileUriException(this,uri);
        return File.OpenRead(Path.Join(uri.Path));
    }

    public Stream OpenWrite(FileUri uri)
    {
        if (uri.Target != Target) throw new UnsupportedFileUriException(this,uri);
        return File.OpenWrite(Path.Join(uri.Path));
    }
}