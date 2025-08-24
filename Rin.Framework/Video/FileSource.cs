namespace Rin.Framework.Video;

public class FileSource(string fileName) : IVideoSource
{
    private readonly FileStream _fileStream = new(fileName, FileMode.Open, FileAccess.Read);

    public void Dispose()
    {
        _fileStream.Dispose();
    }

    public void Read(ulong offset, Span<byte> destination)
    {
        _fileStream.Position = (int)offset;
        _fileStream.ReadExactly(destination);
    }

    public ulong Length => (ulong)_fileStream.Length;
    public ulong Available => Length;
}