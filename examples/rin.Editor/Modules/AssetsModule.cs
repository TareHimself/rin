using rin.Editor.Assets;
using rin.Core;

namespace rin.Editor.Modules;

[NativeRuntimeModule]
public class AssetsModule : RuntimeModule, ISingletonGetter<AssetsModule>
{
    private readonly Dictionary<Type, AssetFactory> _factories = new();

    public static AssetsModule Get()
    {
        return SRuntime.Get().GetModule<AssetsModule>();
    }

    public T AddFactory<T>(T factory) where T : AssetFactory
    {
        _factories.Add(factory.GetAssetType(), factory);
        factory.Start();
        return factory;
    }

    public T NewFactory<T>() where T : AssetFactory
    {
        return AddFactory(Activator.CreateInstance<T>());
    }

    public AssetFactory FactoryFor<T>() where T : Asset
    {
        return _factories[typeof(T)];
    }

    public AssetFactory? FactoryFor(Type t)
    {
        return _factories.GetValueOrDefault(t);
    }
}