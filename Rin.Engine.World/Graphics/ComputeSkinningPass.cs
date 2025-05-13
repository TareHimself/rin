using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.World.Graphics;

public class ComputeSkinningPass(WorldContext worldContext) : IPass
{
    private uint TotalVerticesToSkin { get; set; }

    private Matrix4x4[][] SkinnedPoses { get; set; }

    //private SkinningExecutionInfo[] ExecutionInfos { get; set; }
    private uint SkinnedMeshArrayBufferId { get; set; }
    private uint SkinningExecutionInfoBufferId { get; set; }
    private uint[] SkinningPosesBufferId { get; set; }
    private uint SkinningPoseIdArrayBufferId { get; set; }
    public uint SkinningOutputBufferId { get; set; }
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

    public void Configure(IGraphConfig config)
    {
        //worldContext.ProcessedSkinnedMeshes.
    }

    public void Execute(ICompiledGraph graph, in VkCommandBuffer cmd, Frame frame, IRenderContext context)
    {
        throw new NotImplementedException();
    }
}