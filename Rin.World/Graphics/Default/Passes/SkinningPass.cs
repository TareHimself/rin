using System.Collections.Frozen;
using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.World.Graphics.Mesh;

namespace Rin.World.Graphics.Default.Passes;

/// <summary>
///     Create this pass if we are going to do skinning
/// </summary>
/// <param name="renderContext"></param>
public class SkinningPass(DefaultWorldRenderContext renderContext) : IComputePass
{
    private readonly IComputeShader _skinningShader = IGraphicsModule
        .Get()
        .MakeCompute("World/Shaders/Mesh/Compute/skinning.slang");

    private IMesh[] _skinnedMeshes = [];

    private SkinningExecutionInfo[] ExecutionInfos { get; set; } = [];

    private uint TotalVerticesToSkin { get; set; }

    private Matrix4x4[][] SkinnedPoses { get; set; } = [];

    //private SkinningExecutionInfo[] ExecutionInfos { get; set; }
    private uint SkinnedMeshArrayBufferId { get; set; }
    private uint SkinningExecutionInfoBufferId { get; set; }
    private uint[] SkinningPosesBufferId { get; set; } = [];
    private uint SkinningPoseIdArrayBufferId { get; set; }
    public uint SkinningOutputBufferId { get; set; }
    public uint Id { get; set; }
    public bool IsTerminal => false;
    public Action? OnPrune { get; } = null;

    public void Configure(IGraphConfig config)
    {
        config.DependOn(renderContext.InitPassId);

        _skinnedMeshes = renderContext.SkinnedGeometry.Select(c => c.Mesh).Distinct().ToArray();

        var skinnedGeometryDictionary = _skinnedMeshes
            .Select((c, idx) => new KeyValuePair<IMesh, int>(c, idx)).ToFrozenDictionary();
        TotalVerticesToSkin =
            renderContext.SkinnedGeometry.Aggregate<SkinnedMeshInfo, uint>(0,
                (t, c) => t + c.Mesh.GetVertexCount());
        SkinnedPoses = renderContext.SkinnedGeometry.Select(c => c.Skeleton.ResolvePose(c.Pose).ToArray())
            .ToArray();
        ExecutionInfos = renderContext.SkinnedGeometry.SelectMany((c, poseIdx) =>
        {
            return Enumerable.Range(0, (int)c.Mesh.GetVertexCount()).Select(idx => new SkinningExecutionInfo
            {
                MeshId = skinnedGeometryDictionary[c.Mesh],
                PoseId = poseIdx,
                VertexId = idx
            });
        }).ToArray();

        renderContext.SkinningOutputBufferId =
            config.CreateBuffer<Vertex>(TotalVerticesToSkin, GraphBufferUsage.Compute);
        SkinnedMeshArrayBufferId = config.CreateBuffer<ulong>(_skinnedMeshes.Length, GraphBufferUsage.HostThenCompute);
        SkinningPosesBufferId = SkinnedPoses
            .Select(c => config.CreateBuffer<Matrix4x4>(c.Length, GraphBufferUsage.HostThenCompute)).ToArray();
        SkinningPoseIdArrayBufferId = config.CreateBuffer<ulong>(SkinnedPoses.Length, GraphBufferUsage.HostThenCompute);
        SkinningExecutionInfoBufferId =
            config.CreateBuffer<SkinningExecutionInfo>(ExecutionInfos.Length, GraphBufferUsage.HostThenCompute);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var output = graph.GetBuffer(SkinningOutputBufferId);
        var meshArray = graph.GetBufferOrException(SkinnedMeshArrayBufferId);
        var poseBuffers = SkinningPosesBufferId.Select(graph.GetBufferOrException).ToArray();
        var posesArray = graph.GetBuffer(SkinningPoseIdArrayBufferId);
        var executionInfos = graph.GetBuffer(SkinningExecutionInfoBufferId);

        meshArray.Write(_skinnedMeshes.Select(c => c.GetVertices().GetAddress()).ToArray());
        posesArray.Write(SkinnedPoses.Select((pose, idx) =>
        {
            poseBuffers[idx].Write(pose);
            return poseBuffers[idx].GetAddress();
        }).ToArray());
        executionInfos.Write(ExecutionInfos);

        if (_skinningShader.Bind(ctx) is { } bindContext)
        {
            bindContext
                .Push(new SkinningPushConstants
                {
                    TotalInvocations = (int)TotalVerticesToSkin,
                    MeshesBuffer = meshArray.GetAddress(),
                    PosesBuffer = posesArray.GetAddress(),
                    ExecutionInfoBuffer = executionInfos.GetAddress(),
                    OutputBuffer = output.GetAddress()
                })
                .Invoke(TotalVerticesToSkin);
            //cmd.BufferBarrier(output, MemoryBarrierOptions.ComputeToGraphics());
            ulong offset = 0;
            var skinnedMeshes = renderContext.ProcessedSkinnedMeshes;
            for (var i = 0; i < skinnedMeshes.Length; i++)
            {
                var mesh = skinnedMeshes[i];
                var bufferSize = mesh.VertexBuffer.Size;
                skinnedMeshes[i].VertexBuffer = output.GetView(offset, bufferSize);
                offset += bufferSize;
            }
        }
    }

    private struct SkinningExecutionInfo
    {
        public required int PoseId;
        public required int VertexId;
        public required int MeshId;
    }

    public record struct SkinningPushConstants
    {
        public required ulong ExecutionInfoBuffer;
        public required ulong MeshesBuffer;
        public required ulong OutputBuffer;
        public required ulong PosesBuffer;
        public required int TotalInvocations;
    }
}