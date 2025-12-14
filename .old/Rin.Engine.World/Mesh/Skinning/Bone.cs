using System.Numerics;
using Rin.Framework.Shared.Math;

namespace Rin.Engine.World.Mesh.Skinning;

public class Bone
{
    private Transform? _worldTransform;
    public Bone[] Children = [];
    public string Name { get; set; } = string.Empty;
    public Bone? Parent { get; set; }

    public Matrix4x4 Bind { get; set; }
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