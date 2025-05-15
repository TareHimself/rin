using System.Collections.Frozen;
using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Extensions;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Descriptors;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Meshes;
using Rin.Engine.Graphics.Shaders;
using Rin.Engine.Math;
using Rin.Engine.World.Components;
using Rin.Engine.World.Math;
using Rin.Engine.World.Mesh.Skinning;
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

    private int _firstSkinnedIndex;


    private IMesh[] _skinnedMeshes = [];

    [PublicAPI] public Transform CameraTransform;
    [PublicAPI] public StaticMeshInfo[] Geometry;
    [PublicAPI] public LightInfo[] Lights;
    [PublicAPI] public ProcessedMesh[] ProcessedGeometry;
    [PublicAPI] public SkinnedMeshInfo[] SkinnedGeometry;
    [PublicAPI] public StaticMeshInfo[] StaticGeometry;
    [PublicAPI] public World World;

    /// <summary>
    ///     Collects the scene, and does a depth pre-pass
    /// </summary>
    /// <param name="camera">The perspective the scene is collected from</param>
    /// <param name="size"></param>
    public CollectPass(CameraComponent camera, Vector2<uint> size)
    {
        World = camera.Owner?.World ?? throw new Exception("Camera is not in a scene");
        CameraTransform = camera.GetTransform(Space.World);
        View = (CameraTransform with { Scale = Vector3.One }).ToMatrix().Inverse();
        FieldOfView = camera.FieldOfView;
        NearClip = camera.NearClipPlane;
        FarClip = camera.FarClipPlane;
        Projection = MathR.PerspectiveProjection(FieldOfView, size.X, size.Y,
            NearClip, FarClip);
        ViewProjection = View * Projection;
        Size = size;
        // Collect Scene On Main Thread
        var drawCommands = new CommandList();
        foreach (var root in World.GetPureRoots().ToArray()) root.Collect(drawCommands, World.WorldTransform);
        Geometry = drawCommands.StaticMeshes.ToArray();
        StaticGeometry = drawCommands.StaticMeshes.ToArray();
        SkinnedGeometry = drawCommands.SkinnedMeshes.ToArray();
        Lights = drawCommands.Lights.ToArray();
    }

    /// <summary>
    ///     Depth Image ID
    /// </summary>
    [PublicAPI]
    public uint DepthImageId { get; private set; }

    [PublicAPI] public IGraphImage? DepthImage { get; set; }

    [PublicAPI] public Matrix4x4 View { get; set; }

    [PublicAPI] public Matrix4x4 Projection { get; }

    public Matrix4x4 ViewProjection { get; }
    [PublicAPI] public float FieldOfView { get; set; }
    [PublicAPI] public float NearClip { get; set; }
    [PublicAPI] public float FarClip { get; set; }
    [PublicAPI] public Vector2<uint> Size { get; set; }

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
        DepthImageId = config.CreateImage(Size.X, Size.Y, ImageFormat.Depth, ImageLayout.DepthAttachment);
        DepthSceneBufferId = config.CreateBuffer<DepthSceneInfo>(BufferStage.Graphics);
        var processedMeshes = StaticGeometry.SelectMany(c =>
        {
            return c.SurfaceIndices.Select(idx =>
            {
                var surface = c.Mesh.GetSurface(idx);
                return new ProcessedMesh
                {
                    Transform = c.Transform,
                    IndexBuffer = c.Mesh.GetIndices(),
                    VertexBuffer = c.Mesh.GetVertices(idx),
                    Material = c.Materials[idx],
                    IndicesCount = surface.IndicesCount,
                    IndicesStart = surface.IndicesStart,
                    VertexCount = surface.VertexCount,
                    VertexStart = surface.VertexStart,
                    Bounds = surface.Bounds
                };
            });
        }).ToList();

        // If shader is not ready we can't skin this frame
        if (_skinningShader.Ready)
        {
            _firstSkinnedIndex = processedMeshes.Count;
            processedMeshes.AddRange(SkinnedGeometry.SelectMany(c =>
            {
                return c.SurfaceIndices.Select(idx =>
                {
                    // Need to spoof a regular vertex buffer here since this is a skinned mesh
                    var vertexBuffer = c.Mesh.GetVertices(idx);
                    var offset = vertexBuffer.Offset / Utils.ByteSizeOf<SkinnedVertex>() * Utils.ByteSizeOf<Vertex>();
                    var size = c.Mesh.GetVertexCount(idx) * Utils.ByteSizeOf<Vertex>();
                    var surface = c.Mesh.GetSurface(idx);
                    return new ProcessedMesh
                    {
                        Transform = c.Transform,
                        IndexBuffer = c.Mesh.GetIndices(),
                        VertexBuffer = new SkinnedVertexBufferView(offset, size),
                        Material = c.Materials[idx],
                        IndicesCount = surface.IndicesCount,
                        IndicesStart = surface.IndicesStart,
                        VertexCount = surface.VertexCount,
                        VertexStart = surface.VertexStart,
                        Bounds = c.Mesh.GetBounds()
                    };
                });
            }));

            _skinnedMeshes = SkinnedGeometry.Select(c => c.Mesh).Distinct().ToArray();

            if (_skinnedMeshes.Length > 0)
            {
                var skinnedGeometryDictionary = _skinnedMeshes
                    .Select((c, idx) => new KeyValuePair<IMesh, int>(c, idx)).ToFrozenDictionary();
                TotalVerticesToSkin =
                    SkinnedGeometry.Aggregate<SkinnedMeshInfo, uint>(0, (t, c) => t + c.Mesh.GetVertexCount());
                SkinnedPoses = SkinnedGeometry.Select(c => c.Skeleton.ResolvePose(c.Pose).ToArray()).ToArray();
                ExecutionInfos = SkinnedGeometry.SelectMany((c, poseIdx) =>
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


        ProcessedGeometry = processedMeshes.ToArray();

        var depthMaterialDataSize = ProcessedGeometry.Aggregate(Utils.ByteSizeOf<DepthSceneInfo>(),
            (current, gcmd) => current + gcmd.Material.DepthPass.GetRequiredMemory());

        DepthMaterialBufferId = depthMaterialDataSize > 0
            ? config.CreateBuffer(depthMaterialDataSize, BufferStage.Compute)
            : 0;
    }


    public void Execute(ICompiledGraph graph, in VkCommandBuffer cmd, Frame frame, IRenderContext context)
    {
        if (_skinnedMeshes.NotEmpty()) DoSkinning(graph, frame, cmd);

        var sceneDataBuffer = graph.GetBufferOrException(DepthSceneBufferId);
        var materialDataBuffer = graph.GetBufferOrNull(DepthMaterialBufferId);
        ulong materialDataBufferOffset = 0;
        DepthImage = graph.GetImage(DepthImageId);

        cmd
            .BeginRendering(Size.ToVkExtent(), [], DepthImage.MakeDepthAttachmentInfo(0.0f))
            .SetInputTopology(VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST)
            .SetPolygonMode(VkPolygonMode.VK_POLYGON_MODE_FILL)
            .DisableStencilTest(false)
            .SetCullMode(VkCullModeFlags.VK_CULL_MODE_BACK_BIT, VkFrontFace.VK_FRONT_FACE_CLOCKWISE)
            .EnableDepthTest(true, VkCompareOp.VK_COMPARE_OP_GREATER_OR_EQUAL)
            //.DisableBlending(0, 0)
            .SetVertexInput([], [])
            .SetViewports([
                // For viewport flipping
                new VkViewport
                {
                    x = 0,
                    y = 0,
                    width = Size.X,
                    height = Size.Y,
                    minDepth = 0.0f,
                    maxDepth = 1.0f
                }
            ])
            .SetScissors([
                new VkRect2D
                {
                    offset = new VkOffset2D(),
                    extent = new VkExtent2D
                    {
                        width = Size.X,
                        height = Size.Y
                    }
                }
            ]);

        var sceneFrame = new SceneFrame(frame, View, Projection, sceneDataBuffer, cmd);

        sceneDataBuffer.Write(new DepthSceneInfo
        {
            View = sceneFrame.View,
            Projection = sceneFrame.Projection,
            ViewProjection = sceneFrame.ViewProjection
        });

        foreach (var geometryInfos in ProcessedGeometry.GroupBy(c => c, new ProcessedMesh.CompareByIndexAndMaterial()))
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

    private void DoSkinning(ICompiledGraph graph, Frame frame, in VkCommandBuffer cmd)
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
            cmd.Dispatch(TotalVerticesToSkin);
            //cmd.BufferBarrier(output, MemoryBarrierOptions.ComputeToGraphics());
            ulong offset = 0;
            for (var i = _firstSkinnedIndex; i < ProcessedGeometry.Length; i++)
            {
                var buffer = ProcessedGeometry[i].VertexBuffer as SkinnedVertexBufferView ??
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
        public required int TotalInvocations;
        public required ulong MeshesBuffer;
        public required ulong PosesBuffer;
        public required ulong ExecutionInfoBuffer;
        public required ulong OutputBuffer;
    }
}