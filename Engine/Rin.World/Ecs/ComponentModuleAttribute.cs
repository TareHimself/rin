namespace Rin.World;

/// <summary>
///     Marks the assembly that holds a generated <see cref="ComponentModule" />, so <see cref="ComponentModule.Of" />
///     finds it. The generator derives one per assembly.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public abstract class ComponentModuleAttribute : Attribute
{
    public abstract ComponentModule Module { get; }
}
