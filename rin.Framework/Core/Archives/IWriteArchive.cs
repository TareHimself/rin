namespace rin.Framework.Core.Archives;

public interface IWriteArchive : IArchive
{
    public void Write(string key, Stream data);
}