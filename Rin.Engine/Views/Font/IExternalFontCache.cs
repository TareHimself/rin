namespace Rin.Engine.Views.Font;

public interface IExternalFontCache
{
    public Stream? Get(int id);
    public void Set(int id, Stream data);
    
    public bool SupportsSet { get; }
}