namespace Rin.World.Ecs;

/// <summary>
///     What the store is in the middle of, and so which operations it allows: query loops, hook calls and the
///     listeners before a restore. Each operation has one check.
/// </summary>
/// <remarks>
///     A query loop holds spans into the rows, so rows cannot move. Hooks and despawns run inside an operation that
///     continues after them, so a save would capture half of it and a restore would let it continue on the restored
///     entities. The listeners before a restore must not create entities the restore then replaces.
/// </remarks>
internal sealed class StructuralPhase
{
    private int _iterationDepth;
    private int _hookDepth;

    /// <summary>
    ///     True while the <see cref="IWorldRestoreListener.BeforeRestore" /> listeners run.
    /// </summary>
    public bool IsRestoring { get; private set; }

    public void BeginIteration() => _iterationDepth++;

    public void EndIteration()
    {
        if (_iterationDepth == 0) throw new InvalidOperationException("No query loop is running, so none can end.");
        _iterationDepth--;
    }

    public HookScope EnterHooks()
    {
        _hookDepth++;
        return new HookScope(this);
    }

    public RestoreScope EnterRestore()
    {
        EnsureCanRestore();
        IsRestoring = true;
        return new RestoreScope(this);
    }

    public void EnsureCanSpawn() => ThrowIfRestoring();

    public void EnsureCanReparent() => ThrowIfRestoring();

    /// <summary>
    ///     Adding or removing a component and despawning move rows.
    /// </summary>
    public void EnsureCanMoveRows()
    {
        ThrowIfRestoring();
        if (_iterationDepth > 0)
            throw new InvalidOperationException("Entities cannot change structurally inside a query loop. Use World.Commands.");
    }

    public void EnsureCanSave()
    {
        if (_hookDepth > 0) throw new InvalidOperationException("A snapshot cannot be saved while hooks or a despawn run.");
    }

    public void EnsureCanRestore()
    {
        if (IsRestoring) throw new InvalidOperationException("A snapshot cannot be restored while another restore runs.");
        if (_hookDepth > 0) throw new InvalidOperationException("A snapshot cannot be restored while hooks or a despawn run.");
        if (_iterationDepth > 0) throw new InvalidOperationException("A snapshot cannot be restored inside a query loop.");
    }

    private void ThrowIfRestoring()
    {
        if (IsRestoring) throw new InvalidOperationException("Entities cannot change structurally before a snapshot restore.");
    }

    /// <summary>
    ///     Marks hook calls or a despawn as running until disposed.
    /// </summary>
    public readonly ref struct HookScope(StructuralPhase phase)
    {
        public void Dispose() => phase._hookDepth--;
    }

    /// <summary>
    ///     Marks the listeners before a restore as running until disposed.
    /// </summary>
    public readonly ref struct RestoreScope(StructuralPhase phase)
    {
        public void Dispose() => phase.IsRestoring = false;
    }
}
