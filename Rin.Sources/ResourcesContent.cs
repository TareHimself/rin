using System.Reflection;
using Rin.Sources.Exceptions;

namespace Rin.Sources;

public class ResourcesSource(Assembly assembly,string basePath,string? assemblySuffix = null) : ISource
{
    // public string Target { get; } = 
    //
    // private string MakeResourceName(FileUri uri)
    // {
    //     if (uri.Target != Target) throw new UnsupportedFileUriException(this,uri);
    //     return uri.Path.Aggregate(Target, (t, c) => t + '.' + c);
    // }
    //
    // public bool Exists(FileUri uri)
    // {
    //     return assembly.GetManifestResourceInfo(MakeResourceName(uri)) != null;
    // }
    //
    // public Stream OpenRead(FileUri uri)
    // {
    //     var stream = assembly.GetManifestResourceStream(MakeResourceName(uri));
    //     if(stream == null) throw new Exception("Resource not found");
    //     return stream;
    // }
    //
    // public Stream OpenWrite(FileUri uri)
    // {
    //     throw new WriteNotSupportedException(this, uri);
    // }
        
    // public Stream OpenRead(params string[] name) => OpenRead(new FileUri(Target,name));
    // public Stream OpenWrite(params string[] name) => OpenWrite(new FileUri(Target,name));
    public string BasePath { get; } = basePath;
    private string _assemblyName = assembly.GetName().Name ?? string.Empty;
    private string _assemblySuffix = assemblySuffix ?? ".";

    private string ToResourceName(string path)
    {
        return _assemblyName + _assemblySuffix + string.Join('.',path[(BasePath.Length + 1)..].Split('/'));
    }

    private string FromResourceName(string resourceName)
    {
        var name = resourceName[(_assemblyName.Length + _assemblySuffix.Length)..].Split('.');
        return BasePath + "/" + string.Join('/',name[..^1]) + '.' + name[^1];
    }
    public IEnumerable<string> GetAllResources()
    {
        return assembly.GetManifestResourceNames().Select(FromResourceName);
}
    
    public Stream Read(string path)
    {
        var stream = assembly.GetManifestResourceStream(ToResourceName(path));
        if(stream == null) throw new DoesNotExistException();
        return stream;
    }

    public Stream Write(string path)
    {
        throw new WriteNotSupportedException();
    }
}