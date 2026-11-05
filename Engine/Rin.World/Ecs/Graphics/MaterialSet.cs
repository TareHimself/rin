namespace Rin.World.Ecs.Graphics;

/// <summary>
///     A handle to a material list in <see cref="MaterialSets" />. <c>default</c> is the empty set, which draws every
///     surface with the default material.
/// </summary>
public readonly record struct MaterialSet(int Id);
