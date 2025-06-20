using System.Diagnostics;

namespace Provide;

public class DefaultProvider : IProvider
{
    
    private readonly Dictionary<Type,Func<IProvider,object>> _singles = [];
    private readonly Dictionary<Type,Func<IProvider,object>> _multiples = [];
    private readonly Dictionary<Type,object> _singletons = [];
    
    public IProvider AddSingle<TInterface>(Func<IProvider,TInterface> constructor) where TInterface : class
    {
        _singles.Add(typeof(TInterface), constructor);
        return this;
    }

    public IProvider AddMulti<TInterface>(Func<IProvider,TInterface> constructor) where TInterface : class
    {
        _multiples.Add(typeof(TInterface), constructor);
        return this;
    }


    public TInterface Get<TInterface>() where TInterface : class
    {
        var type = typeof(TInterface);
        
        Debug.Assert(_singles.ContainsKey(type) || _multiples.ContainsKey(type),$"No factory found for type {type}");

        {
            if (_singles.TryGetValue(type, out var ctor))
            {
                {
                    if (_singletons.TryGetValue(type, out var obj))
                    {
                        return (TInterface)obj;
                    }
                    
                    var instance = ctor.Invoke(this);
                    _singletons[type] = instance;
                    return (TInterface)instance;
                }
            }
        }

        {
            if (_multiples.TryGetValue(typeof(TInterface), out var ctor))
            {
                return (TInterface)ctor.Invoke(this);
            }
        }

        throw new NullReferenceException();
    }
}