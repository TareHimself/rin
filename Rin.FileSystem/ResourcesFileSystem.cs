using System.Reflection;
using Rin.FileSystem.Exceptions;

namespace Rin.FileSystem;

public class ResourcesFileSystem(Assembly assembly) : IFileSystem
{
    public string Target { get; } = assembly.GetName().Name ?? string.Empty;

    private string MakeResourceName(FileUri uri)
    {
        if (uri.Target != Target) throw new UnsupportedFileUriException(this,uri);
        return uri.Path.Aggregate(Target, (t, c) => t + '.' + c);
    }
    
    public bool Exists(FileUri uri)
    {
        return assembly.GetManifestResourceInfo(MakeResourceName(uri)) != null;
    }

    public Stream OpenRead(FileUri uri)
    {
        var stream = assembly.GetManifestResourceStream(MakeResourceName(uri));
        if(stream == null) throw new Exception("Resource not found");
        return stream;
    }

    public Stream OpenWrite(FileUri uri)
    {
        throw new WriteNotSupportedException(this, uri);
    }

    public IEnumerable<FileUri> GetAllResources()
    {
        return assembly.GetManifestResourceNames().Select(c => new FileUri(Target,c[(Target.Length + 1)..]));
    }
    
        
    public Stream OpenRead(params string[] name) => OpenRead(new FileUri(Target,name));
    public Stream OpenWrite(params string[] name) => OpenWrite(new FileUri(Target,name));
}