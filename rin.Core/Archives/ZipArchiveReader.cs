using SharpCompress.Common;
using SharpCompress.Readers;

namespace rin.Core.Archives;

using scZip = SharpCompress.Archives.Zip.ZipArchive;
public class ZipArchiveReader(string fileName) : ArchiveReader
{
    private readonly scZip _zipArchive = scZip.Open(fileName);
    public override IEnumerable<string> Keys => _zipArchive.Entries.Select(c => c.Key ?? "");
    public override int Count => _zipArchive.Entries.Count;

    protected override void OnDispose(bool isManual)
    {
        _zipArchive.Dispose();
    }

    

    public override Stream CreateReadStream(string key)
    {
        var target = _zipArchive.Entries.SingleOrDefault(c => c.Key == key);

        if (target == null) throw new Exception("Key does not exist");

        return target.OpenEntryStream();
    }
}