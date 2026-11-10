using System.Collections.Frozen;
using System.Numerics;
using JetBrains.Annotations;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Core.Shared;
using Rin.Shade;
using Rin.World.Graphics.Default.Shaders;
using Rin.World.Mesh.Skinning;
using Rin.World.Graphics.Mesh;

namespace Rin.World.Graphics.Default.Passes;

/// <summary>
///     Create this pass if we are going to do skinning
/// </summary>
/// <param name="sceneFrame"></param>
public partial class SkinningPass(DefaultSceneFrame sceneFrame) : IComputePass
{
    [ComputeShader<SkinningShader>]
    private partial IComputeShader Shader { get; }
    
    private List<SkinningExecutionInfo> ExecutionInfos { get; set; } = [];

    private uint TotalVerticesToSkin { get; set; }

    //private SkinningExecutionInfo[] ExecutionInfos { get; set; }
    private uint SkinnedMeshArrayBufferId { get; set; }
    private uint SkinningExecutionInfoBufferId { get; set; }
    private List<uint> SkinningPoseBufferIds { get; set; } = new(sceneFrame.SkinnedGeometry.Length);
    private uint PosePointerArrayBufferId { get; set; }
    private List<IMesh> UniqueSkinnedMeshes { get; set; } = [];
    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        TotalVerticesToSkin = (uint)sceneFrame.SkinnedVertexCount;
        sceneFrame.SkinningOutputBufferId = config.CreateBuffer<Vertex>(TotalVerticesToSkin, GraphBufferUsage.Compute);
        
        HashSet<uint> sourceVertexReads = [];
        Dictionary<IMesh,int> uniqueMeshesDictionary = [];
        UniqueSkinnedMeshes.EnsureCapacity(sceneFrame.ProcessedSkinnedMeshCount);
        ExecutionInfos.EnsureCapacity(sceneFrame.SkinnedSurfaceCount);
        
        // Buffers for skinning poses
        foreach (var skinnedPose in sceneFrame.SkinningMatrices)
        {
            SkinningPoseBufferIds.Add(config.CreateBuffer<Matrix4x4>(skinnedPose.Count, GraphBufferUsage.HostThenCompute));
        }
        
        // Execution infos and distinct meshes
        for (var i = sceneFrame.SkinnedMeshStartIndex; i < sceneFrame.ProcessedMeshes.Count; i++)
        {

            var processedMesh = sceneFrame.ProcessedMeshes[i];
            if (sourceVertexReads.Add(processedMesh.SourceVertexBufferId))
                config.ReadBuffer(processedMesh.SourceVertexBufferId, GraphBufferUsage.Compute);
            var geometry = sceneFrame.SkinnedGeometry[processedMesh.GeometryId];
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
        
        SkinnedMeshArrayBufferId = config.CreateBuffer<DeviceSpan<SkinnedVertex>>(UniqueSkinnedMeshes.Count, GraphBufferUsage.HostThenCompute);
        PosePointerArrayBufferId = config.CreateBuffer<DeviceSpan<Matrix4x4>>(SkinningPoseBufferIds.Count, GraphBufferUsage.HostThenCompute);
        SkinningExecutionInfoBufferId =
            config.CreateBuffer<SkinningExecutionInfo>(ExecutionInfos.Count, GraphBufferUsage.HostThenCompute);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var output = graph.GetBuffer(sceneFrame.SkinningOutputBufferId);
        var meshPointersArray = graph.GetBufferOrException(SkinnedMeshArrayBufferId);
        var posePointerArray = graph.GetBuffer(PosePointerArrayBufferId);
        {
            ulong offset = 0;
            for (var i = 0; i < SkinningPoseBufferIds.Count; i++)
            {
                var bufferId = SkinningPoseBufferIds[i];
                var pose = sceneFrame.SkinningMatrices[i];
                var buffer = graph.GetBufferOrException(bufferId);
                buffer.Write(pose);
                offset += posePointerArray.WriteSingle(buffer.ToDeviceSpan<Matrix4x4>(), offset);
            }
        }
        var executionInfos = graph.GetBuffer(SkinningExecutionInfoBufferId);
        executionInfos.Write(ExecutionInfos);
        using var meshPointers = new PooledMemory<DeviceSpan<SkinnedVertex>>(UniqueSkinnedMeshes.Count);
        for (var i = 0; i < UniqueSkinnedMeshes.Count; i++) meshPointers[i] = UniqueSkinnedMeshes[i].GetVertices().ToDeviceSpan<SkinnedVertex>();
        meshPointersArray.Write(meshPointers);

        if (Shader.Bind(ctx) is { } bindContext)
        {
            bindContext
                .Push(new SkinningShader.PushConstants
                {
                    TotalInvocations = (int)TotalVerticesToSkin,
                    Meshes = meshPointersArray.ToDeviceSpan<DeviceSpan<SkinnedVertex>>(),
                    Poses = posePointerArray.ToDeviceSpan<DeviceSpan<Matrix4x4>>(),
                    ExecutionInfo = executionInfos.ToDeviceSpan<SkinningExecutionInfo>(),
                    Output = output.ToDeviceSpan<Vertex>()
                })
                .Invoke(TotalVerticesToSkin);
            //cmd.BufferBarrier(output, MemoryBarrierOptions.ComputeToGraphics());
            for (var i = sceneFrame.SkinnedMeshStartIndex; i < sceneFrame.ProcessedMeshes.Count; i++)
            {
                var mesh = sceneFrame.ProcessedMeshes[i];
                mesh.VertexBuffer = output.GetView(mesh.VertexBuffer.Offset, mesh.VertexBuffer.Size);
            }
        }
    }
}