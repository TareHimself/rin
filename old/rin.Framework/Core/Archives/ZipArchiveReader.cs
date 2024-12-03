using SharpCompress.Common;
using SharpCompress.Readers;

namespace rin.Framework.Core.Archives;

using scZip = SharpCompress.Archives.Zip.ZipArchive;
public class ZipArchiveReader(string fileName) : IArchiveReader
{
    private readonly scZip _zipArchive = scZip.Open(fileName);
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
}