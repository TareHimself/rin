namespace Rin.World.Scripting;

/// <summary>
///     Slot of the entity's script list. Removing it detaches every script.
/// </summary>
public readonly struct ScriptRef : IDerivedComponentData
{
    private readonly int _slotPlusOne;

    internal ScriptRef(int slot) => _slotPlusOne = slot + 1;

    internal int Slot => _slotPlusOne - 1;

    /// <summary>
    ///     False for a cleared reference, which a restore left without a script.
    /// </summary>
    internal bool IsBound => _slotPlusOne != 0;
}
