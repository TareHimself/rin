using rin.Core;

namespace rin.Editor.Assets;

public abstract class AssetFactory : Disposable
{
    public virtual void Start()
    {
    }

    protected override void OnDispose(bool isManual)
    {
        throw new NotImplementedException();
    }

    public abstract Type GetAssetType();
    public abstract Type GetLoadType();

    public abstract object? Load(Asset asset);

    public abstract Task<Asset?> Import(string filePath);

    public abstract Task<bool> Export(string filePath, Asset asset);

    public abstract bool CanImport();

    public abstract bool CanExport();
}