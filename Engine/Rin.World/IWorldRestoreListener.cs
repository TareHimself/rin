namespace Rin.World;

/// <summary>
///     Rebuilds state outside the entity store around <see cref="World.RestoreSnapshot" />. Restore fires no
///     <see cref="World.OnAdd{T}" /> or <see cref="World.OnRemove{T}" /> hooks, so a feature that creates handles from
///     hooks registers a listener with <see cref="World.AddRestoreListener" /> instead.
/// </summary>
public interface IWorldRestoreListener
{
    /// <summary>
    ///     Called while the pre-restore entities still exist, to release what they own.
    /// </summary>
    void BeforeRestore();

    /// <summary>
    ///     Called once the restored entities exist, in registration order, to rebuild every
    ///     <see cref="IDerivedComponentData" /> component.
    /// </summary>
    void AfterRestore();
}
