using System.Numerics;
using Rin.Shade;

namespace Rin.World.Graphics.Surfaces;

public struct SurfaceDepthVertexOut
{
    [Position] public Vector4 Position;
}

/// <summary>
///     The depth prepass for a material that moves vertices: runs its
///     <see cref="ISurfaceMaterial{TSelf}.ProcessVertex" /> exactly like <see cref="SurfaceShader{TMaterial}" /> does.
///     Declare <c>[Shader] public partial class GoldDepthShader : SurfaceDepthShader&lt;Gold&gt;;</c> and return it from
///     <see cref="ISurfaceMaterial{TSelf}.DepthShader" />.
/// </summary>
[ShadeExport]
public abstract class SurfaceDepthShader<TMaterial> : Shader where TMaterial : unmanaged, ISurfaceMaterial<TMaterial>
{
    public struct PushConstants
    {
        public BufferRef<DepthSceneInfo> Scene;
        public BufferRef<SurfaceDraw<TMaterial>> Draws;
    }

    [Push] protected PushConstants Push;

    [Vertex, Depth]
    public SurfaceDepthVertexOut Vertex(SurfaceVertexIn input)
    {
        var draw = Push.Draws[input.InstanceId];
        var scene = Push.Scene[0];
        var world = TMaterial.ProcessVertex(draw.Material, SurfaceVertices.Load(draw.Instance, input.VertexId),
            scene.Time);

        SurfaceDepthVertexOut output;
        output.Position = SurfaceVertices.Project(world.Position, scene.View, scene.Projection);
        return output;
    }
}
