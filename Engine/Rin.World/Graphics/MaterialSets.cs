
namespace Rin.World.Graphics;

/// <summary>
///     Keeps managed material lists out of components. Index <c>i</c> is surface <c>i</c>; a missing or null entry
///     reuses the previous surface's material.
/// </summary>
public sealed class MaterialSets
{
    private readonly List<IMeshMaterial?[]> _sets = [[]];

    public MaterialSet Add(params IMeshMaterial?[] materials)
    {
        _sets.Add(materials);
        return new MaterialSet(_sets.Count - 1);
    }

    public IMeshMaterial?[] Get(MaterialSet set) => _sets[set.Id];
}
