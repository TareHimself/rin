namespace Rin.Core.Shared.Video;

public class FileVideoSource(string fileName) : IVideoSource
{
    private readonly FileStream _fileStream = new(fileName, FileMode.Open, FileAccess.Read);

    public void Dispose()
    {
        _fileStream.Dispose();
    }

    public void Read(long offset, Span<byte> destination)
    {
        _fileStream.Position = offset;
        _fileStream.ReadExactly(destination);
    }

    public long Length => _fileStream.Length;
    public long Available => Length;
}
