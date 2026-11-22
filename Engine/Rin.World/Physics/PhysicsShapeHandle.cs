namespace Rin.World.Physics;

/// <summary>
///     Refers to a compound's parts in <see cref="PhysicsShapes" />. The default handle refers to nothing.
/// </summary>
public readonly record struct PhysicsShapeHandle(uint Id)
{
    public bool IsValid => Id != 0;
}
