namespace rin.Framework.Core.Archives;

public interface IArchiveWriter : IArchive
{
    public void Write(string key,Stream data);
}