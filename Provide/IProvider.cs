namespace Provide;

public interface IProvider
{
    IProvider AddSingle<TInterface>(Func<IProvider,TInterface> constructor) where TInterface : class;
    IProvider AddMulti<TInterface>(Func<IProvider,TInterface> constructor) where TInterface : class;
    
    TInterface Get<TInterface>() where TInterface : class;
}