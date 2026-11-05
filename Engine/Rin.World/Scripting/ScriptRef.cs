namespace Rin.World.Scripting;

/// <summary>
///     Added by <see cref="ScriptFeature.Attach" />: the slot of the entity's <see cref="Script" /> in the feature.
///     Removing it, or despawning the entity, detaches the script. A snapshot restore clears it and the feature binds it
///     again.
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
