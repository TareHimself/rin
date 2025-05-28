using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;
namespace Rin.Engine.World.Graphics;

public class FillGBufferIndirectPass : IPass
{
    private readonly WorldContext _worldContext;
    private uint _worldBufferId;

    public FillGBufferIndirectPass(WorldContext worldContext)
    {
        _worldContext = worldContext;
    }

    public uint Id { get; set; }
    public bool IsTerminal => false;
    public bool HandlesPreAdd => false;
    public bool HandlesPostAdd => false;

    public void PreAdd(IGraphBuilder builder)
    {
        throw new NotImplementedException();
    }

    public void PostAdd(IGraphBuilder builder)
    {
        throw new NotImplementedException();
    }

    private uint[] _materialBufferIds;
    public void Configure(IGraphConfig config)
    {
        config.ReadImage(_worldContext.DepthImageId, ImageLayout.DepthAttachment);
        config.WriteImage(_worldContext.GBufferImage0,ImageLayout.ColorAttachment);
        config.WriteImage(_worldContext.GBufferImage1,ImageLayout.ColorAttachment);
        config.WriteImage(_worldContext.GBufferImage2,ImageLayout.ColorAttachment);
        if (_worldContext.SkinningOutputBufferId > 0)
            config.ReadBuffer(_worldContext.SkinningOutputBufferId,GraphBufferUsage.Graphics);
        
        _worldBufferId = config.CreateBuffer<WorldInfo>(GraphBufferUsage.HostThenGraphics);

        var indirectGroups = _worldContext.IndirectGroups;
        
        _materialBufferIds = new uint[indirectGroups.Length];
        
        for (var i = 0; i < _materialBufferIds.Length; i++)
        {
            var group = indirectGroups[i];
            var size = group.First().Material.ColorPass.GetRequiredMemory() * (ulong)group.Length;
            if (size > 0)
            {
                _materialBufferIds[i] = config.CreateBuffer(size,
                    GraphBufferUsage.HostThenGraphics);
            }
        }
        
        foreach (var id in _worldContext.IndirectCommandBuffers)
        {
            config.ReadBuffer(id, GraphBufferUsage.Indirect);
        }
        foreach (var id in _worldContext.IndirectCommandCountBuffers)
        {
            config.ReadBuffer(id, GraphBufferUsage.Indirect);
        }
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var gBuffer0 = graph.GetImageOrException(_worldContext.GBufferImage0);
        var gBuffer1 = graph.GetImageOrException(_worldContext.GBufferImage1);
        var gBuffer2 = graph.GetImageOrException(_worldContext.GBufferImage2);
        var depthImage = graph.GetImageOrException(_worldContext.DepthImageId);
        var materialDataBuffers = _materialBufferIds.Select(graph.GetBufferOrNull).ToArray();
        var indirectCommandBuffers = _worldContext.IndirectCommandBuffers.Select(graph.GetBufferOrException).ToArray();
        var indirectCommandCountBuffers = _worldContext.IndirectCommandCountBuffers.Select(graph.GetBufferOrException).ToArray();
        var worldBuffer = graph.GetBufferOrException(_worldBufferId);
        
        var extent = _worldContext.Extent;
        ctx
            .BeginRendering(extent, [gBuffer0, gBuffer1, gBuffer2], depthAttachment: depthImage)
            .EnableBackFaceCulling()
            .DisableDepthWrite();

        var worldFrame = new WorldFrame(_worldContext.View, _worldContext.Projection, worldBuffer, ctx);

        worldBuffer.Write(new WorldInfo
        {
            View = worldFrame.View,
            Projection = worldFrame.Projection,
            ViewProjection = worldFrame.ViewProjection,
            CameraPosition = _worldContext.ViewTransform.Position
        });
        
        var indirectGroups = _worldContext.IndirectGroups;
        for (var i = 0; i < indirectGroups.Length; i++)
        {
            var group = indirectGroups[i];
            var materialDataBuffer = materialDataBuffers[i];
            var commandBuffer = indirectCommandBuffers[i];
            var countBuffer = indirectCommandCountBuffers[i];
            var first = group.First();
            var firstPass = first.Material.ColorPass;
            if (materialDataBuffer != null)
            {
                var dataSize = firstPass.GetRequiredMemory();
                ulong offset = 0;
                foreach (var mesh in group)
                {
                    mesh.Material.ColorPass.Write(materialDataBuffer.GetView(offset,dataSize),mesh);
                    offset += dataSize;
                }
            }
            ctx.BindIndexBuffer(first.IndexBuffer);
            if (firstPass.BindAndPush(worldFrame, materialDataBuffer))
            {
                ctx.DrawIndexedIndirectCount(commandBuffer, countBuffer, (uint)group.Length, 0);
            }
        }
        ctx.EndRendering();
    }
}