using SharpCompress.Common;
using SharpCompress.Writers;

namespace aerox.Runtime.Archives;

using scZip = SharpCompress.Archives.Zip.ZipArchive;
public class ZipArchiveWriter : ArchiveWriter
{
    private readonly scZip _zipArchive = scZip.Create();
    protected override void OnDispose(bool isManual)
    {
        _zipArchive.Dispose();
    }

    public override void Write(string key, Stream data)
    {
        _zipArchive.AddEntry(key, data);
    }

    public virtual void SaveTo(Stream target,CompressionType compressionType = CompressionType.None)
    {
        _zipArchive.SaveTo(target,new WriterOptions(compressionType));
    }
}