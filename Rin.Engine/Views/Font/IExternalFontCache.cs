namespace Rin.Engine.Views.Font;

public interface IExternalFontCache
{
    public bool SupportsSet { get; }
    public Stream? Get(int id);
    public void Set(int id, Stream data);
}