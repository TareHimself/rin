using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Rin.Engine.Core;

public class FileUri
{
    private static readonly string SchemeDelimiter = "://";

    private static readonly string PathDelimiter = "/";
    
    public static readonly string SystemScheme = "system";
        
    [PublicAPI]
    public string Scheme { get; private set; }

    [PublicAPI]
    public string[] Path { get; private set; }
        
    public FileUri([StringSyntax(StringSyntaxAttribute.Uri)] string uri)
    {
        var startIdx = uri.IndexOf(SchemeDelimiter, StringComparison.Ordinal);
        Scheme = uri[..startIdx];
        var path = uri[(startIdx + SchemeDelimiter.Length)..];
        Path = path.Split(PathDelimiter, StringSplitOptions.RemoveEmptyEntries);
    }
        
        
    public FileUri([StringSyntax(StringSyntaxAttribute.Uri)] string scheme,string [] path)
    {
        Scheme = scheme;
        Path = path;
    }

    public static FileUri FromSystemPath(string path) => new FileUri(SystemScheme,path.Contains('/') ? path.Split('/') : path.Split('\\'));
}