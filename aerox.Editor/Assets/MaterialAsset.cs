using aerox.Editor.Modules;

namespace aerox.Editor.Assets;

public class MaterialAsset : Asset
{
    public override string GetDisplayName()
    {
        return "MaterialInstance";
    }

    public override AssetFactory GetFactory()
    {
        return AssetsModule.Get().FactoryFor<MaterialAsset>();
    }
}