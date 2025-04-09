using System.Numerics;
using Rin.Engine.World.Math;

namespace Rin.Engine.World.Skinning;

public class Bone
{
    public string Name { get; set; } = string.Empty;
    public Bone? Parent { get; set; }
    public Bone[] Children = [];
    public Transform LocalTransform { get; set; }
}