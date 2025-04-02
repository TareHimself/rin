namespace Rin.Shading;

public interface IFileSystem
{
    public string GetUniqueIdentifier(string fullSourcePath);
    public string GetFullSourcePath(string path);
    public string GetFullIncludePath(string fullSourcePath, string includePath);
    public Stream GetContent(string fullSourcePath);
}