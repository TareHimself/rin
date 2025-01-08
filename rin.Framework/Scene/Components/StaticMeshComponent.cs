using rin.Framework.Core.Math;
using rin.Framework.Scene.Entities;
using rin.Framework.Scene.Graphics;

namespace rin.Framework.Scene.Components;

public class StaticMeshComponent : RenderedComponent
{
    public StaticMesh? Mesh { get; set; }
    protected override void CollectSelf(DrawCommands drawCommands, Mat4 transform)
    {
        throw new NotImplementedException();
    }
}