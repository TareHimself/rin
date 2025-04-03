namespace Rin.Engine.Archives;

public interface IWriteArchive : IArchive
{
    public void Write(string key, Stream data);
}