using rin.Graphics.Material;

namespace rin.Editor.Assets;

public class MaterialFactory : AssetFactory
{
    public override Type GetAssetType()
    {
        return typeof(MaterialAsset);
    }

    public override Type GetLoadType()
    {
        return typeof(MaterialInstance);
    }

    public override object? Load(Asset asset)
    {
        throw new NotImplementedException();
    }

    public override Task<Asset?> Import(string filePath)
    {
        throw new NotImplementedException();
    }

    public override Task<bool> Export(string filePath, Asset asset)
    {
        throw new NotImplementedException();
    }

    public override bool CanImport()
    {
        return true;
    }

    public override bool CanExport()
    {
        return true;
    }
}