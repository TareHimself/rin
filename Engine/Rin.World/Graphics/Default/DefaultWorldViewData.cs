using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Shared.Math;
using Rin.World.Components;
using Rin.World.Graphics.Default.Passes;
using Rin.World.Math;

namespace Rin.World.Graphics.Default;

/// <summary>
///     One view of a <see cref="DefaultWorldSnapshot" />: camera, render targets and the view-dependent passes
///     (culling onward). The scene's view-independent work is shared through <see cref="SceneFrame" />.
/// </summary>
public class DefaultWorldViewData : IWorldCollectedData
{
    public DefaultWorldViewData(CameraComponent viewer, in Extent2D extent, DefaultWorldSnapshot snapshot)
    {
        Snapshot = snapshot;
        ViewTransform = viewer.GetTransform(Space.World) with { Scale = Vector3.One };
        View = ViewTransform.ToMatrix().Inverse();
        FieldOfView = viewer.FieldOfView;
        NearClip = viewer.NearClipPlane;
        FarClip = viewer.FarClipPlane;
        Extent = extent;
        Projection = MathR.PerspectiveProjection(FieldOfView, extent, NearClip, FarClip);
        ViewProjection = View * Projection;
        ViewFrustum = MathR.ExtractWorldSpaceFrustum(View, Projection, ViewProjection);
    }

    [PublicAPI] public DefaultWorldSnapshot Snapshot { get; }

    /// <summary>
    ///     How this view issues mesh draws. Resolved against the device when the view is written to a graph.
    /// </summary>
    public MeshDrawMode DrawMode { get; init; } = MeshDrawMode.Auto;

    /// <summary>The scene's shared half inside the graph currently being written; set by <see cref="Write" />.</summary>
    public DefaultSceneFrame SceneFrame { get; private set; } = null!;

    [PublicAPI] public Transform ViewTransform;
    [PublicAPI] public Frustum ViewFrustum;
    [PublicAPI] public Matrix4x4 View { get; }
    [PublicAPI] public Matrix4x4 Projection { get; }
    [PublicAPI] public Matrix4x4 ViewProjection { get; }
    [PublicAPI] public float FieldOfView { get; }
    [PublicAPI] public float NearClip { get; }
    [PublicAPI] public float FarClip { get; }
    [PublicAPI] public Extent2D Extent { get; }

    public LightInfo[] Lights => Snapshot.Lights;

    public uint DepthImageId { get; set; }
    public uint GBufferImage0 { get; set; }
    public uint GBufferImage1 { get; set; }
    public uint GBufferImage2 { get; set; }
    public uint GBufferImage3 { get; set; }
    public uint OutputImageId { get; set; }

    public uint[] IndirectCommandBuffers { get; set; } = [];
    public uint[] IndirectCommandCountBuffers { get; set; } = [];
    public uint[] DepthIndirectCommandBuffers { get; set; } = [];
    public uint[] DepthIndirectCommandCountBuffers { get; set; } = [];

    public uint GetOutputImageId()
    {
        return OutputImageId;
    }

    public uint GetGBufferImageId(int index)
    {
        return index switch
        {
            0 => GBufferImage0,
            1 => GBufferImage1,
            2 => GBufferImage2,
            3 => GBufferImage3,
            _ => 0
        };
    }

    public LightInfo[] GetLights()
    {
        return Lights;
    }

    public void Write(IGraphBuilder builder)
    {
        SceneFrame = Snapshot.Write(builder);

        builder.AddPass(new InitViewResourcesPass(this));
        var useIndirect = UseIndirectDrawing(DrawMode, IGraphicsModule.Get().CurrentDevice.SupportsIndirectRendering);
        foreach (var pass in CreateGeometryPasses(useIndirect)) builder.AddPass(pass);
        builder.AddPass(new LightingPass(this));
    }

    /// <summary>
    ///     Whether a view in <paramref name="mode" /> uses indirect draws on a device with the given support.
    /// </summary>
    public static bool UseIndirectDrawing(MeshDrawMode mode, bool deviceSupportsIndirect)
    {
        return mode switch
        {
            MeshDrawMode.Indirect => true,
            MeshDrawMode.Direct => false,
            _ => deviceSupportsIndirect
        };
    }

    /// <summary>
    ///     The passes that cull, build draw commands and draw the scene's meshes into the depth and G-buffer images.
    ///     The direct set has no culling or command passes.
    /// </summary>
    public IReadOnlyList<IPass> CreateGeometryPasses(bool useIndirect)
    {
        if (!useIndirect) return [new DepthPrepassDirectPass(this), new FillGBufferDirectPass(this)];

        var cullingPass = new CullingPass(this);
        return
        [
            cullingPass,
            new FillIndirectBuffersPass(cullingPass, this),
            new DepthPrepassIndirectPass(this),
            new FillGBufferIndirectPass(this)
        ];
    }
}
