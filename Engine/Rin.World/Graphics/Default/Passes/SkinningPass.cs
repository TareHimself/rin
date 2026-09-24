using System.Collections.Frozen;
using System.Numerics;
using JetBrains.Annotations;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Core.Shared;
using Rin.World.Graphics.Mesh;

namespace Rin.World.Graphics.Default.Passes;

/// <summary>
///     Create this pass if we are going to do skinning
/// </summary>
/// <param name="collectedData"></param>
public partial class SkinningPass(DefaultWorldCollectedData collectedData) : IComputePass
{
    [ComputeShader("Shaders/World/Mesh/Compute/skinning.slang")]
    private partial IComputeShader SkinningShader { get; }
    
    private List<SkinningExecutionInfo> ExecutionInfos { get; set; } = [];

    private uint TotalVerticesToSkin { get; set; }

    //private SkinningExecutionInfo[] ExecutionInfos { get; set; }
    private uint SkinnedMeshArrayBufferId { get; set; }
    private uint SkinningExecutionInfoBufferId { get; set; }
    private List<uint> SkinningPoseBufferIds { get; set; } = new(collectedData.SkinnedPoses.Count);
    private uint PosePointerArrayBufferId { get; set; }
    private List<IMesh> UniqueSkinnedMeshes { get; set; } = [];
    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        TotalVerticesToSkin = (uint)collectedData.SkinnedVertexCount;
        collectedData.SkinningOutputBufferId = config.CreateBuffer<Vertex>(TotalVerticesToSkin, GraphBufferUsage.Compute);
        
        HashSet<uint> sourceVertexReads = [];
        Dictionary<IMesh,int> uniqueMeshesDictionary = [];
        UniqueSkinnedMeshes.EnsureCapacity(collectedData.ProcessedSkinnedMeshCount);
        ExecutionInfos.EnsureCapacity(collectedData.SkinnedSurfaceCount);
        
        // Buffers for skinning poses
        foreach (var skinnedPose in collectedData.SkinnedPoses)
        {
            SkinningPoseBufferIds.Add(config.CreateBuffer<Matrix4x4>(skinnedPose.Count, GraphBufferUsage.HostThenCompute));
        }
        
        // Execution infos and distinct meshes
        for (var i = collectedData.SkinnedMeshStartIndex; i < collectedData.ProcessedMeshes.Count; i++)
        {

            var processedMesh = collectedData.ProcessedMeshes[i];
            if (sourceVertexReads.Add(processedMesh.SourceVertexBufferId))
                config.ReadBuffer(processedMesh.SourceVertexBufferId, GraphBufferUsage.Compute);
            var geometry = collectedData.SkinnedGeometry[processedMesh.GeometryId];
            var mesh = geometry.Mesh;
            
            if (!uniqueMeshesDictionary.TryGetValue(mesh, out var uniqueMeshIndex))
            {
                uniqueMeshIndex = uniqueMeshesDictionary.Count;
                uniqueMeshesDictionary.Add(mesh, uniqueMeshIndex);
                UniqueSkinnedMeshes.Add(mesh);
            }
            
            
            
            for (var vertexId = 0; vertexId < processedMesh.Surface.VertexCount; vertexId++)
            {
                ExecutionInfos.Add(new SkinningExecutionInfo
                {
                    MeshIndex = uniqueMeshIndex,
                    PoseIndex = processedMesh.GeometryId, // poses are 1-1 with the skinned geometry array
                    VertexIndex = (int)(processedMesh.Surface.VertexStart + vertexId) // Vertex index is buffer relative
                });
            }
        }
        
        SkinnedMeshArrayBufferId = config.CreateBuffer<ulong>(UniqueSkinnedMeshes.Count, GraphBufferUsage.HostThenCompute);
        PosePointerArrayBufferId = config.CreateBuffer<ulong>(SkinningPoseBufferIds.Count, GraphBufferUsage.HostThenCompute);
        SkinningExecutionInfoBufferId =
            config.CreateBuffer<SkinningExecutionInfo>(ExecutionInfos.Count, GraphBufferUsage.HostThenCompute);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var output = graph.GetBuffer(collectedData.SkinningOutputBufferId);
        var meshPointersArray = graph.GetBufferOrException(SkinnedMeshArrayBufferId);
        var posePointerArray = graph.GetBuffer(PosePointerArrayBufferId);
        {
            ulong offset = 0;
            for (var i = 0; i < SkinningPoseBufferIds.Count; i++)
            {
                var bufferId = SkinningPoseBufferIds[i];
                var pose = collectedData.SkinnedPoses[i];
                var buffer = graph.GetBufferOrException(bufferId);
                buffer.Write(pose);
                var ptr = buffer.GetAddress();
                offset += posePointerArray.WriteSingle(ptr, offset);
            }
        }
        var executionInfos = graph.GetBuffer(SkinningExecutionInfoBufferId);
        executionInfos.Write(ExecutionInfos);
        meshPointersArray.Write(UniqueSkinnedMeshes.Select(c => c.GetVertices().GetAddress()).ToArray());

        if (SkinningShader.Bind(ctx) is { } bindContext)
        {
            bindContext
                .Push(new SkinningPushConstants
                {
                    TotalInvocations = (int)TotalVerticesToSkin,
                    MeshesBuffer = meshPointersArray.GetAddress(),
                    PosesBuffer = posePointerArray.GetAddress(),
                    ExecutionInfoBuffer = executionInfos.GetAddress(),
                    OutputBuffer = output.GetAddress()
                })
                .Invoke(TotalVerticesToSkin);
            //cmd.BufferBarrier(output, MemoryBarrierOptions.ComputeToGraphics());
            for (var i = collectedData.SkinnedMeshStartIndex; i < collectedData.ProcessedMeshes.Count; i++)
            {
                var mesh = collectedData.ProcessedMeshes[i];
                mesh.VertexBuffer = output.GetView(mesh.VertexBuffer.Offset, mesh.VertexBuffer.Size);
            }
        }
    }
    
    [NoReorder]
    private struct SkinningExecutionInfo
    {
        public required int PoseIndex;
        public required int MeshIndex;
        public required int VertexIndex;
        
    }
    [NoReorder]
    public record struct SkinningPushConstants
    {
        public required int TotalInvocations;
        public required ulong MeshesBuffer;
        public required ulong PosesBuffer;
        public required ulong ExecutionInfoBuffer;
        public required ulong OutputBuffer;
    }
}