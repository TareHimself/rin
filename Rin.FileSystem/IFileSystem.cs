namespace Rin.FileSystem;

public interface IFileSystem
{
    public string Target { get; }
    public bool Exists(FileUri uri);
    public Stream OpenRead(FileUri uri);
    public Stream OpenWrite(FileUri uri);

}