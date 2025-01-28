namespace rin.Framework.Core;

public interface IFileSystem
{
    public bool Exists(string path);
    public Stream OpenRead(string path);
    public Stream OpenWrite(string path);
    
    public string ReadAllText(string path);
}