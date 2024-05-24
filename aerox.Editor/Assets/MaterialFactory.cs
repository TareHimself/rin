using aerox.Runtime.Graphics.Material;

namespace aerox.Editor.Assets;

public class MaterialFactory : AssetFactory
{
    public override Type GetAssetType() => typeof(MaterialAsset);

    public override Type GetLoadType() => typeof(MaterialInstance);

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

    public override bool CanImport() => true;

    public override bool CanExport() => true;
}