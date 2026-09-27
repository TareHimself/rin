using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Shared;

namespace Rin.World.Graphics.Default.Passes;

public class InitSceneResourcesPass(DefaultSceneFrame sceneFrame) : IPass
{
    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        sceneFrame.BoundsBufferId =
            config.CreateBuffer<Bounds3D>(sceneFrame.ProcessedMeshes.Count, GraphBufferUsage.Host);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var meshes = sceneFrame.ProcessedMeshes;
        using var bounds = new PooledMemory<Bounds3D>(meshes.Count);
        for (var i = 0; i < meshes.Count; i++) bounds[i] = meshes[i].Surface.Bounds;
        graph.GetBufferOrException(sceneFrame.BoundsBufferId).Write(bounds);
    }
}
