using System.Diagnostics;

namespace Rin.Framework;

public class DefaultProvider : IProvider
{
    private readonly Dictionary<Type, Func<IProvider, object>> _singleFactories = [];
    private readonly Dictionary<Type, Func<IProvider, object>> _factories = [];
    private readonly Dictionary<Type, object> _instances = [];

    public IProvider AddSingle<TInterface>(Func<IProvider, TInterface> factory) where TInterface : class
    {
        var type = typeof(TInterface);
        Debug.Assert(!_singleFactories.ContainsKey(type), "Cannot replace singleton when an instance exists");
        _singleFactories[type] = factory;
        return this;
    }

    public TInterface AddSingle<TInterface>(TInterface instance) where TInterface : class
    {
        _instances[typeof(TInterface)] = instance;
        return instance;
    }

    public IProvider Add<TInterface>(Func<IProvider, TInterface> factory) where TInterface : class
    {
        _factories[typeof(TInterface)] = factory;
        return this;
    }

    public IProvider ClearSingle<TInterface>() where TInterface : class
    {
        _instances.Remove(typeof(TInterface));
        return this;
    }

    public IProvider RemoveSingle<TInterface>() where TInterface : class
    {
        _singleFactories.Remove(typeof(TInterface));
        _instances.Remove(typeof(TInterface));
        return this;
    }

    public IProvider Remove<TInterface>() where TInterface : class
    {
        _factories.Remove(typeof(TInterface));
        return this;
    }
    
    public TInterface Get<TInterface>() where TInterface : class
    {
        var type = typeof(TInterface);

        Debug.Assert(_factories.ContainsKey(type) || _singleFactories.ContainsKey(type) || _instances.ContainsKey(type), $"No factory found for type {type}");
        
        {
            if (_instances.TryGetValue(type, out var instance))
            {
                return (TInterface)instance;
            }
        }

        {
            if (_singleFactories.TryGetValue(type, out var factory))
            {
                return (TInterface)(_instances[type] = factory(this));
            }
        }
        
        {
            if (_factories.TryGetValue(type, out var factory))
            {
                return (TInterface)factory(this);
            }
        }

        throw new NullReferenceException();
    }
}