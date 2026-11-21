namespace Rin.World;

/// <summary>
///     Leaves a component's values out of every save, such as a <see cref="WorldSnapshot" />. A restore zeroes them, so
///     data that holds handles into a subsystem is rebuilt by its feature from an <see cref="IWorldRestoreListener" />.
/// </summary>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class EcsNotSavedAttribute : Attribute;
