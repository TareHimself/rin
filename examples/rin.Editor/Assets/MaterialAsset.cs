using rin.Editor.Modules;

namespace rin.Editor.Assets;

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