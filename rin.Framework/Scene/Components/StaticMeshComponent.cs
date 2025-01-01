using rin.Framework.Core.Math;
using rin.Framework.Scene.Entities;
using rin.Framework.Scene.Graphics;

namespace rin.Framework.Scene.Components;

[Component(typeof (TransformComponent))]
public class StaticMeshComponent(Entity owner) : Component(owner)
{
    public StaticMesh? Mesh { get; set; }
}