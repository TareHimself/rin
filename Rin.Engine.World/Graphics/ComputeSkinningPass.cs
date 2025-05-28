using System.Collections.Frozen;
using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Meshes;
using Rin.Engine.Graphics.Shaders;

namespace Rin.Engine.World.Graphics;

public class ComputeSkinningPass(WorldContext worldContext) : IComputePass
{
    private readonly IComputeShader _skinningShader = SGraphicsModule
        .Get()
        .MakeCompute("World/Shaders/Mesh/compute_skinning.slang");
    
    private IMesh[] _skinnedMeshes = [];

    private SkinningExecutionInfo[] ExecutionInfos { get; set; } = [];
    
    private uint TotalVerticesToSkin { get; set; }

    private Matrix4x4[][] SkinnedPoses { get; set; } = [];

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
        config.DependOn(worldContext.InitPassId);
        
        _skinnedMeshes = worldContext.SkinnedGeometry.Select(c => c.Mesh).Distinct().ToArray();

        if (_skinnedMeshes.Length > 0)
        {
            var skinnedGeometryDictionary = _skinnedMeshes
                .Select((c, idx) => new KeyValuePair<IMesh, int>(c, idx)).ToFrozenDictionary();
            TotalVerticesToSkin =
                worldContext.SkinnedGeometry.Aggregate<SkinnedMeshInfo, uint>(0,
                    (t, c) => t + c.Mesh.GetVertexCount());
            SkinnedPoses = worldContext.SkinnedGeometry.Select(c => c.Skeleton.ResolvePose(c.Pose).ToArray())
                .ToArray();
            ExecutionInfos = worldContext.SkinnedGeometry.SelectMany((c, poseIdx) =>
            {
                return Enumerable.Range(0, (int)c.Mesh.GetVertexCount()).Select(idx => new SkinningExecutionInfo
                {
                    MeshId = skinnedGeometryDictionary[c.Mesh],
                    PoseId = poseIdx,
                    VertexId = idx
                });
            }).ToArray();

            SkinningOutputBufferId = config.CreateBuffer<Vertex>(TotalVerticesToSkin,GraphBufferUsage.Compute);
            SkinnedMeshArrayBufferId = config.CreateBuffer<ulong>(_skinnedMeshes.Length,GraphBufferUsage.HostThenCompute);
            SkinningPosesBufferId = SkinnedPoses
                .Select(c => config.CreateBuffer<Matrix4x4>(c.Length,GraphBufferUsage.HostThenCompute)).ToArray();
            SkinningPoseIdArrayBufferId = config.CreateBuffer<ulong>(SkinnedPoses.Length,GraphBufferUsage.HostThenCompute);
            SkinningExecutionInfoBufferId =
                config.CreateBuffer<SkinningExecutionInfo>(ExecutionInfos.Length, GraphBufferUsage.HostThenCompute);
        }
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var output = graph.GetBuffer(SkinningOutputBufferId);
        var meshArray = graph.GetBufferOrException(SkinnedMeshArrayBufferId);
        var poseBuffers = SkinningPosesBufferId.Select(graph.GetBufferOrException).ToArray();
        var posesArray = graph.GetBuffer(SkinningPoseIdArrayBufferId);
        var executionInfos = graph.GetBuffer(SkinningExecutionInfoBufferId);

        meshArray.Write(_skinnedMeshes.Select(c => c.GetVertices().GetAddress()));
        posesArray.Write(SkinnedPoses.Select((pose, idx) =>
        {
            poseBuffers[idx].Write(pose);
            return poseBuffers[idx].GetAddress();
        }));
        executionInfos.Write(ExecutionInfos);

        if (_skinningShader.Bind(ctx))
        {
            _skinningShader.Push(ctx,
                new SkinningPushConstants
                {
                    TotalInvocations = (int)TotalVerticesToSkin,
                    MeshesBuffer = meshArray.GetAddress(),
                    PosesBuffer = posesArray.GetAddress(),
                    ExecutionInfoBuffer = executionInfos.GetAddress(),
                    OutputBuffer = output.GetAddress()
                });
            ctx.Invoke(_skinningShader, TotalVerticesToSkin);
            //cmd.BufferBarrier(output, MemoryBarrierOptions.ComputeToGraphics());
            ulong offset = 0;
            var skinnedMeshes = worldContext.ProcessedSkinnedMeshes;
            for (var i = 0; i < skinnedMeshes.Length; i++)
            {
                var buffer = skinnedMeshes[i].VertexBuffer as SkinnedVertexBufferView ??
                             throw new InvalidCastException();
                buffer.UnderlyingView = output.GetView(offset, buffer.Size);
                offset += buffer.Size;
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