namespace rin.Core.Archives;

public abstract class ArchiveWriter : Archive
{
    public abstract void Write(string key,Stream data);
}