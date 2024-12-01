using SharpCompress.Common;
using SharpCompress.Writers;

namespace rin.Framework.Core.Archives;

using scZip = SharpCompress.Archives.Zip.ZipArchive;

public class ZipArchiveWriter : IArchiveWriter
{
    private readonly scZip _zipArchive = scZip.Create();
    
    public void Dispose()
    {
        _zipArchive.Dispose();
    }

    public void Write(string key, Stream data)
    {
        _zipArchive.AddEntry(key, data);
    }

    public virtual void SaveTo(Stream target,CompressionType compressionType = CompressionType.None)
    {
        _zipArchive.SaveTo(target,new WriterOptions(compressionType));
    }
}