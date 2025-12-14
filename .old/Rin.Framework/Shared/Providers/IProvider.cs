namespace Rin.Framework.Shared.Providers;

public interface IProvider
{
    /// <summary>
    /// Add a factory for a class that will only be constructed once
    /// </summary>
    /// <param name="factory"></param>
    /// <typeparam name="TInterface"></typeparam>
    /// <returns></returns>
    IProvider AddSingle<TInterface>(Func<IProvider, TInterface> factory) where TInterface : class;

    /// <summary>
    /// Add a class
    /// </summary>
    /// <param name="instance"></param>
    /// <typeparam name="TInterface"></typeparam>
    /// <returns></returns>
    TInterface AddSingle<TInterface>(TInterface instance) where TInterface : class;

    /// <summary>
    /// Add a factory for a class that will be created with every call to <see cref="Get{TInterface}"/>
    /// </summary>
    /// <param name="factory"></param>
    /// <typeparam name="TInterface"></typeparam>
    /// <returns></returns>
    IProvider Add<TInterface>(Func<IProvider, TInterface> factory) where TInterface : class;

    /// <summary>
    /// Clears the current instance of the singleton if one exists
    /// </summary>
    /// <typeparam name="TInterface"></typeparam>
    /// <returns></returns>
    IProvider ClearSingle<TInterface>() where TInterface : class;

    IProvider RemoveSingle<TInterface>() where TInterface : class;

    /// <summary>
    /// Removes the factory for the <see cref="TInterface"/>
    /// </summary>
    /// <typeparam name="TInterface"></typeparam>
    /// <returns></returns>
    IProvider Remove<TInterface>() where TInterface : class;
    TInterface Get<TInterface>() where TInterface : class;
}