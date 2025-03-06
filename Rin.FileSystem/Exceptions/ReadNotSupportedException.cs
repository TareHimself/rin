namespace Rin.FileSystem.Exceptions;

public class ReadNotSupportedException(IFileSystem fileSystem,FileUri uri,string? message = null) : Exception(message)
{
    public IFileSystem FileSystem { get; } = fileSystem;
    public FileUri Uri { get; } = uri;
}