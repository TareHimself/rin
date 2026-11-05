namespace Rin.World;

/// <summary>
///     Marks component data that holds handles into a subsystem, such as a render proxy or a physics body. Its bytes are
///     never saved in a <see cref="WorldSnapshot" />: a restore clears it and the owning feature rebuilds it from an
///     <see cref="IWorldRestoreListener" />.
/// </summary>
public interface IDerivedComponentData : IComponentData;
