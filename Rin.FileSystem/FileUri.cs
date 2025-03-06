using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Rin.FileSystem;

public class FileUri
{
    private static readonly string TargetDelimiter = ":/";

    private static readonly string PathDelimiter = "/";
        
    [PublicAPI]
    public string Target { get; private set; }

    [PublicAPI]
    public string[] Path { get; private set; }
        
    public FileUri([StringSyntax(StringSyntaxAttribute.Uri)] string uri)
    {
        var startIdx = uri.IndexOf(TargetDelimiter, StringComparison.Ordinal);
        Target = uri[..startIdx];
        var path = uri[(startIdx + TargetDelimiter.Length)..];
        Path = path.Split(PathDelimiter, StringSplitOptions.RemoveEmptyEntries);
    }
        
        
    public FileUri([StringSyntax(StringSyntaxAttribute.Uri)] string target,string [] path)
    {
        Target = target;
        Path = path;
    }
    
    public FileUri([StringSyntax(StringSyntaxAttribute.Uri)] string target,string path)
    {
        Target = target;
        Path = path.Contains('/') ? path.Split('/') : path.Split('\\');
    }

    //public static FileUri FromSystemPath(string path) => new FileUri(SystemScheme,path);
}