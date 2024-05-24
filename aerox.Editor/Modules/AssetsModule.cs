
using aerox.Editor.Assets;
using aerox.Runtime;
using aerox.Runtime.Graphics;
using aerox.Runtime.Scene;

namespace aerox.Editor.Modules;


[NativeRuntimeModule]
public class AssetsModule : RuntimeModule,ISingletonGetter<AssetsModule>
{
    private readonly Dictionary<Type, AssetFactory> _factories = new();
    
    public static AssetsModule Get()
    {
        return Runtime.Runtime.Instance.GetModule<AssetsModule>();
    }
    
    public T AddFactory<T>(T factory) where T : AssetFactory
    {
        _factories.Add(factory.GetAssetType(),factory);
        factory.Start();
        return factory;
    }

    public T NewFactory<T>() where T : AssetFactory => AddFactory(Activator.CreateInstance<T>());

    public AssetFactory FactoryFor<T>() where T : Asset
    {
        return _factories[typeof(T)];
    }
    
    public AssetFactory? FactoryFor(Type t)
    {
        return _factories.GetValueOrDefault(t);
    }
    
    
}