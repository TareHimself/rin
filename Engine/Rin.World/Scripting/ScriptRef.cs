namespace Rin.World.Scripting;

/// <summary>
///     Added by <see cref="ScriptFeature.Attach" />: the slot of the entity's <see cref="Script" /> in the feature.
///     Removing it, or despawning the entity, detaches the script.
/// </summary>
public readonly struct ScriptRef : IComponentData
{
    internal ScriptRef(int slot) => Slot = slot;

    internal int Slot { get; }
}
