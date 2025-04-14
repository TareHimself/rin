using Rin.Engine.Math;
using Rin.Engine.World.Math;

namespace Rin.Engine.World.Skinning;

public class Bone
{
    public Bone[] Children = [];
    public string Name { get; set; } = string.Empty;
    
    public Bone? Parent { get; set; }
    public Transform LocalTransform { get; set; }
    
    private Transform? _worldTransform;

    public Transform WorldTransform
    {
        get
        {
            if (!_worldTransform.HasValue)
            {
                _worldTransform = Parent == null ? LocalTransform : LocalTransform.InParentSpace(Parent.WorldTransform);
            }
            
            return _worldTransform.Value;
        }
    }
}