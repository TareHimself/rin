namespace Rin.World.Physics;

/// <summary>
///     The parts of every compound collider, shared by any number of bodies. Registered compounds live as long as the
///     registry, like level content, so snapshots only need the handle.
/// </summary>
public sealed class PhysicsShapes
{
    private readonly List<ShapePart[]> _compounds = [];

    public PhysicsShapeHandle AddCompound(ReadOnlySpan<ShapePart> parts)
    {
        if (parts.IsEmpty) throw new ArgumentException("A compound needs at least one part.", nameof(parts));
        foreach (var part in parts)
            if (part.Shape.Kind == ColliderShapeKind.Compound)
                throw new ArgumentException("A compound's parts must be boxes, spheres or capsules.", nameof(parts));

        _compounds.Add(parts.ToArray());
        return new PhysicsShapeHandle((uint)_compounds.Count);
    }

    public ReadOnlySpan<ShapePart> PartsOf(PhysicsShapeHandle handle) =>
        handle.IsValid && handle.Id <= _compounds.Count
            ? _compounds[(int)handle.Id - 1]
            : throw new ArgumentException($"{handle} is not a registered compound.", nameof(handle));
}
