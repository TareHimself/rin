using System.Numerics;
using Rin.Core.Shared.Math;

namespace Rin.World.Mesh.Skinning;

public class Bone
{
    private Transform? _worldTransform;
    public Bone[] Children = [];
    public string Name { get; set; } = string.Empty;
    public Bone? Parent { get; set; }

    // Matrix4x4's implicit default is the all-zero matrix, not identity - explicit default here so a
    // hand-built bone (no glTF skin behind it) skins as identity instead of collapsing to the origin.
    public Matrix4x4 Bind { get; set; } = Matrix4x4.Identity;
    public Transform LocalTransform { get; set; }

    public Transform WorldTransform
    {
        get
        {
            if (!_worldTransform.HasValue)
                _worldTransform = Parent == null ? LocalTransform : LocalTransform.InParentSpace(Parent.WorldTransform);

            return _worldTransform.Value;
        }
    }
}