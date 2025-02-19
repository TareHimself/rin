using SharpCompress.Common;
using SharpCompress.Writers;

namespace rin.Framework.Core.Archives;

using scZip = SharpCompress.Archives.Zip.ZipArchive;

public class ZipArchive : IReadArchive, IWriteArchive
{
    private readonly scZip _zipArchive;

    public ZipArchive(Stream data)
    {
        _zipArchive = scZip.Open(data);
    }

    public ZipArchive()
    {
        _zipArchive = scZip.Create();
    }

    public IEnumerable<string> Keys => _zipArchive.Entries.Select(c => c.Key ?? "");
    public int Count => _zipArchive.Entries.Count;

    public Stream CreateReadStream(string key)
    {
        var target = _zipArchive.Entries.SingleOrDefault(c => c.Key == key);

        if (target == null) throw new Exception("Key does not exist");

        return target.OpenEntryStream();
    }

    public void Dispose()
    {
        _zipArchive.Dispose();
    }

    public void Write(string key, Stream data)
    {
        _zipArchive.AddEntry(key, data);
    }

    public virtual void SaveTo(Stream target, CompressionType compressionType = CompressionType.None)
    {
        _zipArchive.SaveTo(target, new WriterOptions(compressionType));
    }
}