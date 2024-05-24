namespace aerox.Editor.Assets;


public abstract class Asset
{

    public abstract string GetDisplayName();
    public abstract AssetFactory GetFactory();

    public virtual object? Resolve() => GetFactory().Load(this);
}