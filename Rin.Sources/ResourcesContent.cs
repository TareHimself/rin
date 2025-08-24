using System.Reflection;
using System.Text;
using Rin.Sources.Exceptions;

namespace Rin.Sources;

public class AssemblyResource : ISource
{
    private readonly string _assemblyPath;
    private readonly Assembly _assembly;

    public AssemblyResource(Assembly assembly, string alias, string? rootAssemblyPath = null)
    {
        _assembly = assembly;
        
        var assemblyName = assembly.GetName().Name ?? string.Empty;
        var rootPathStr = (rootAssemblyPath ?? string.Empty).TrimEnd();
        var suffix = rootPathStr.Length == 0 ? string.Empty : ".";
        _assemblyPath = new StringBuilder().Append(assemblyName).Append('.').Append(string.Join('.',rootPathStr.Split('/', StringSplitOptions.RemoveEmptyEntries))).Append(suffix).ToString();
        BasePath = alias;
    }

    public string BasePath { get; }

    public Stream Read(string path)
    {
        var resourceName = ToResourceName(path);
        var stream = _assembly.GetManifestResourceStream(resourceName);
        if (stream == null) throw new DoesNotExistException();
        return stream;
    }

    public Stream Write(string path)
    {
        throw new WriteNotSupportedException();
    }

    private string ToResourceName(string path)
    {
        return _assemblyPath + string.Join('.', path[(BasePath.Length + 1)..].Split('/'));
    }

    private string FromResourceName(string resourceName)
    {
        var name = resourceName[_assemblyPath.Length..].Split('.',StringSplitOptions.RemoveEmptyEntries);//; resourceName[(_assemblyName.Length + _assemblySuffix.Length)..].Split('.');
        return new StringBuilder().Append(BasePath).Append('/').Append(string.Join('/', name[..^1])).Append('.')
            .Append(name[^1]).ToString();
    }

    public IEnumerable<string> GetAllResources()
    {
        return _assembly.GetManifestResourceNames().Select(FromResourceName);
    }
    
    public static AssemblyResource New<TAssemblyType>(string basePath,string? rootAssemblyPath = null)
    {
        return new AssemblyResource(typeof(TAssemblyType).Assembly, basePath,rootAssemblyPath);
    }
}