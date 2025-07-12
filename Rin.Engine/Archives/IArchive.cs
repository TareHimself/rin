namespace Rin.Engine.Archives;

public interface IArchive : IDisposable
{
    // public bool ReadSupported { get; }
    // public bool WriteSupported { get; }
    public bool Contains(string id);
}