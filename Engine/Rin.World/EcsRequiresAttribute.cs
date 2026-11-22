namespace Rin.World;

/// <summary>
///     Declares that a component only works alongside <typeparamref name="T" />, so editors add it first and refuse
///     to remove it while this component is there.
/// </summary>
/// <remarks>
///     The component module generator reads it into <see cref="ComponentInfo.Requires" />. Requirements go one way:
///     a cycle makes <see cref="ComponentSchema.Compose" /> throw.
/// </remarks>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
public sealed class EcsRequiresAttribute<T> : Attribute where T : unmanaged, IComponentData;
