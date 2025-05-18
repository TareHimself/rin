using System.Collections.Frozen;
using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Extensions;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Meshes;
using Rin.Engine.Graphics.Shaders;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.World.Graphics;

/// <summary>
///     Collects the <see cref="Rin.Engine.World.World" />, performs skinning and does a depth pre-pass
/// </summary>
public class CollectPass : IPass
{
    private readonly IComputeShader _skinningShader = SGraphicsModule
        .Get()
        .MakeCompute("World/Shaders/Mesh/compute_skinning.slang");

    private readonly WorldContext _worldContext;


    private IMesh[] _skinnedMeshes;

    public CollectPass(WorldContext worldContext)
    {
        _worldContext = worldContext;
    }

    [PublicAPI] public uint DepthImageId { get; private set; }

    [PublicAPI] public IGraphImage? DepthImage { get; set; }
    private uint TotalVerticesToSkin { get; set; }
    private Matrix4x4[][] SkinnedPoses { get; set; }
    private SkinningExecutionInfo[] ExecutionInfos { get; set; }
    private uint SkinnedMeshArrayBufferId { get; set; }
    private uint SkinningExecutionInfoBufferId { get; set; }
    private uint[] SkinningPosesBufferId { get; set; }
    private uint SkinningPoseIdArrayBufferId { get; set; }
    public uint SkinningOutputBufferId { get; set; }
    private uint DepthSceneBufferId { get; set; }
    private uint DepthMaterialBufferId { get; set; }

    public void PreAdd(IGraphBuilder builder)
    {
        throw new NotImplementedException();
    }

    public void PostAdd(IGraphBuilder builder)
    {
    }


    public void Configure(IGraphConfig config)
    {
        _worldContext.ProcessMeshes();

        DepthImageId = _worldContext.DepthImageId =
            config.CreateImage(_worldContext.Extent, ImageFormat.Depth, ImageLayout.DepthAttachment);
        DepthSceneBufferId = config.CreateBuffer<DepthSceneInfo>(BufferStage.Graphics);
        // If shader is not ready we can't skin this frame
        if (_skinningShader.Ready)
        {
            _skinnedMeshes = _worldContext.SkinnedGeometry.Select(c => c.Mesh).Distinct().ToArray();

            if (_skinnedMeshes.Length > 0)
            {
                var skinnedGeometryDictionary = _skinnedMeshes
                    .Select((c, idx) => new KeyValuePair<IMesh, int>(c, idx)).ToFrozenDictionary();
                TotalVerticesToSkin =
                    _worldContext.SkinnedGeometry.Aggregate<SkinnedMeshInfo, uint>(0,
                        (t, c) => t + c.Mesh.GetVertexCount());
                SkinnedPoses = _worldContext.SkinnedGeometry.Select(c => c.Skeleton.ResolvePose(c.Pose).ToArray())
                    .ToArray();
                ExecutionInfos = _worldContext.SkinnedGeometry.SelectMany((c, poseIdx) =>
                {
                    return Enumerable.Range(0, (int)c.Mesh.GetVertexCount()).Select(idx => new SkinningExecutionInfo
                    {
                        MeshId = skinnedGeometryDictionary[c.Mesh],
                        PoseId = poseIdx,
                        VertexId = idx
                    });
                }).ToArray();

                SkinningOutputBufferId = config.CreateBuffer<Vertex>(TotalVerticesToSkin, BufferStage.Compute);
                SkinnedMeshArrayBufferId = config.CreateBuffer<ulong>(_skinnedMeshes.Length, BufferStage.Compute);
                SkinningPosesBufferId = SkinnedPoses
                    .Select(c => config.CreateBuffer<Matrix4x4>(c.Length, BufferStage.Compute)).ToArray();
                SkinningPoseIdArrayBufferId = config.CreateBuffer<ulong>(SkinnedPoses.Length, BufferStage.Compute);
                SkinningExecutionInfoBufferId =
                    config.CreateBuffer<SkinningExecutionInfo>(ExecutionInfos.Length, BufferStage.Compute);
            }
        }
        else
        {
            _skinnedMeshes = [];
            _worldContext.ProcessedSkinnedMeshes = [];
        }

        var depthMaterialDataSize = _worldContext.ProcessedMeshes.Aggregate(Utils.ByteSizeOf<DepthSceneInfo>(),
            (current, gcmd) => current + gcmd.Material.DepthPass.GetRequiredMemory());

        DepthMaterialBufferId = depthMaterialDataSize > 0
            ? config.CreateBuffer(depthMaterialDataSize, BufferStage.Compute)
            : 0;
    }


    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var cmd = ctx.GetCommandBuffer();
        if (_skinnedMeshes.NotEmpty()) DoSkinning(graph, ctx, cmd);

        var sceneDataBuffer = graph.GetBufferOrException(DepthSceneBufferId);
        var materialDataBuffer = graph.GetBufferOrNull(DepthMaterialBufferId);
        ulong materialDataBufferOffset = 0;
        DepthImage = graph.GetImage(DepthImageId);
        var extent = _worldContext.Extent;
        cmd
            .BeginRendering(extent, [], DepthImage.MakeDepthAttachmentInfo(0.0f))
            .DisableStencilTest()
            .SetCullMode(VkCullModeFlags.VK_CULL_MODE_BACK_BIT, VkFrontFace.VK_FRONT_FACE_CLOCKWISE)
            .EnableDepthTest(true)
            .SetViewports([
                // For viewport flipping
                new VkViewport
                {
                    x = 0,
                    y = 0,
                    width = extent.Width,
                    height = extent.Height,
                    minDepth = 0.0f,
                    maxDepth = 1.0f
                }
            ])
            .SetScissors([
                new VkRect2D
                {
                    offset = new VkOffset2D(),
                    extent = extent.ToVk()
                }
            ]);

        var sceneFrame = new WorldFrame(_worldContext.View, _worldContext.Projection, sceneDataBuffer, cmd);

        sceneDataBuffer.Write(new DepthSceneInfo
        {
            View = sceneFrame.View,
            Projection = sceneFrame.Projection,
            ViewProjection = sceneFrame.ViewProjection
        });

        foreach (var geometryInfos in _worldContext.ProcessedMeshes.GroupBy(c => c,
                     new ProcessedMesh.CompareByIndexAndMaterial()))
        {
            var infos = geometryInfos.ToArray();
            var first = infos.First();
            var size = first.Material.DepthPass.GetRequiredMemory() * (ulong)infos.Length;
            var view = materialDataBuffer?.GetView(materialDataBufferOffset, size);
            materialDataBufferOffset += size;
            first.Material.DepthPass.Execute(sceneFrame, view, infos);
        }

        cmd.EndRendering();
    }

    public uint Id { get; set; }
    public bool IsTerminal { get; set; } = false;
    public bool HandlesPreAdd => false;
    public bool HandlesPostAdd => false;

    private void DoSkinning(ICompiledGraph graph, IExecutionContext ctx, in VkCommandBuffer cmd)
    {
        // SkinningOutputBufferId = config.AllocateBuffer<Vertex>(totalVertices);
        // SkinnedMeshArrayBufferId = config.AllocateBuffer<ulong>(SkinnedMeshes.Length);
        // SkinningPosesBufferId = config.AllocateBuffer<Matrix4x4>(SkinnedPoses.Aggregate(0,(t,c) => t + c.Length));
        // SkinningPoseArrayBufferId = config.AllocateBuffer<ulong>(SkinnedPoses.Length);
        // SkinningExecutionInfoBufferId = config.AllocateBuffer<SkinningExecutionInfo>(ExecutionInfos.Length);
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

        if (_skinningShader.Bind(cmd))
        {
            _skinningShader.Push(cmd, new SkinningPushConstants
            {
                TotalInvocations = (int)TotalVerticesToSkin,
                MeshesBuffer = meshArray.GetAddress(),
                PosesBuffer = posesArray.GetAddress(),
                ExecutionInfoBuffer = executionInfos.GetAddress(),
                OutputBuffer = output.GetAddress()
            });
            _skinningShader.Invoke(cmd, TotalVerticesToSkin);
            //cmd.BufferBarrier(output, MemoryBarrierOptions.ComputeToGraphics());
            ulong offset = 0;
            var skinnedMeshes = _worldContext.ProcessedSkinnedMeshes;
            for (var i = 0; i < skinnedMeshes.Length; i++)
            {
                var buffer = skinnedMeshes[i].VertexBuffer as SkinnedVertexBufferView ??
                             throw new InvalidCastException();
                buffer.UnderlyingView = output.GetView(offset, buffer.Size);
                offset += buffer.Size;
            }
        }
    }

    public void Dispose()
    {
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