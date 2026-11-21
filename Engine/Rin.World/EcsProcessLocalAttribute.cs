namespace Rin.World;

/// <summary>
///     Marks a field whose value only means something in the running process, like a handle into a subsystem.
/// </summary>
/// <remarks>
///     A <see cref="WorldSnapshot" /> keeps it, since a restore runs in the same process. Anything that leaves the process,
///     like a scene file, leaves it out, and the schema does not require it to be a known field type.
/// </remarks>
[AttributeUsage(AttributeTargets.Field)]
public sealed class EcsProcessLocalAttribute : Attribute;
