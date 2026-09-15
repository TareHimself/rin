namespace Rin.Core;

public interface IProviderResolvable<TSelf> where TSelf : class, IProviderResolvable<TSelf>
{
    private static TSelf? _override;

    /// <summary>
    ///     Bypasses <see cref="Global.Provider" /> entirely when set - lets tests inject a fake without
    ///     registering it on the provider. Reset to <see langword="null" /> to restore normal resolution.
    /// </summary>
    static TSelf? Override
    {
        get => _override;
        set => _override = value;
    }

    static TSelf Get()
    {
        return _override ?? Global.Provider.Get<TSelf>();
    }
}
