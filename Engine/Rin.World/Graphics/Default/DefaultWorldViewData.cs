using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Shared.Math;
using Rin.Shade;
using Rin.World.Graphics.Default.Passes;
using Rin.World.Graphics.Default.Shaders;
using Rin.World.Graphics.ImageBasedLighting;

namespace Rin.World.Graphics.Default;

/// <summary>
///     One view of a <see cref="DefaultWorldSnapshot" />: camera, render targets and the view-dependent passes
///     (culling onward). The scene's view-independent work is shared through <see cref="SceneFrame" />.
/// </summary>
public class DefaultWorldViewData : IWorldCollectedData
{
    private bool _disposed;

    public DefaultWorldViewData(in CameraView viewer, in Extent2D extent, DefaultWorldSnapshot snapshot)
    {
        Snapshot = snapshot;
        snapshot.Acquire();
        ViewTransform = Transform.From(viewer.World) with { Scale = Vector3.One };
        View = ViewTransform.ToMatrix().Inverse();
        FieldOfView = viewer.FieldOfView;
        NearClip = viewer.NearClipPlane;
        FarClip = viewer.FarClipPlane;
        Channel = viewer.Channel;
        Exposure = viewer.Exposure;
        ToneMapCurve = viewer.ToneMapCurve;
        Extent = extent;
        Projection = MathR.PerspectiveProjection(FieldOfView, extent, NearClip);
        Environment = EnvironmentSelector.Select(snapshot.Environments, ViewTransform.Position);
    }

    [PublicAPI] public DefaultWorldSnapshot Snapshot { get; }

    /// <summary>
    ///     How this view issues mesh draws. Resolved against the device when the view is written to a graph.
    /// </summary>
    public MeshDrawMode DrawMode { get; init; } = MeshDrawMode.Auto;

    public float Time { get; init; }

    /// <summary>
    ///     The scene's shared half inside the graph currently being written; set by <see cref="Write" />.
    /// </summary>
    public DefaultSceneFrame SceneFrame { get; private set; } = null!;

    [PublicAPI] public Transform ViewTransform;
    [PublicAPI] public Matrix4x4 View { get; }
    [PublicAPI] public Matrix4x4 Projection { get; }
    [PublicAPI] public float FieldOfView { get; }
    [PublicAPI] public float NearClip { get; }
    [PublicAPI] public float FarClip { get; }
    [PublicAPI] public Extent2D Extent { get; }
    [PublicAPI] public RenderChannel Channel { get; }
    [PublicAPI] public float Exposure { get; }
    [PublicAPI] public ToneMapCurve ToneMapCurve { get; }

    public ReadOnlySpan<LightInfo> Lights => Snapshot.Lights;

    public ViewEnvironment Environment { get; }

    public uint DepthImageId { get; set; }

    /// <summary>
    ///     Linear, unclamped colour the forward and sky passes draw into.
    /// </summary>
    public uint SceneColorImageId { get; set; }

    /// <summary>
    ///     What the viewport shows: the tone mapped scene, or the scene colour itself for a debug channel.
    /// </summary>
    public uint OutputImageId { get; set; }

    /// <summary>
    ///     Whether the view holds scene brightness that needs tone mapping. The debug channels hold display values.
    /// </summary>
    public bool ToneMaps => Channel == RenderChannel.Scene;

    public uint LightGridBufferId { get; set; }
    public uint ClusterItemsBufferId { get; set; }
    public uint PointLightsBufferId { get; set; }
    public uint DirectionalLightsBufferId { get; set; }
    public uint EnvironmentVolumesBufferId { get; set; }
    public uint LightBinsBufferId { get; set; }
    public uint TileMasksBufferId { get; set; }

    public uint[] IndirectCommandBuffers { get; set; } = [];
    public uint[] IndirectCommandCountBuffers { get; set; } = [];
    public uint[] DepthIndirectCommandBuffers { get; set; } = [];
    public uint[] DepthIndirectCommandCountBuffers { get; set; } = [];

    public uint GetOutputImageId()
    {
        return OutputImageId;
    }

    /// <summary>
    ///     Declares a graphics read of every light grid buffer, for a pass that shades with the grid.
    /// </summary>
    public void ReadLightGrid(IGraphConfig config)
    {
        config.ReadBuffer(LightGridBufferId, GraphBufferUsage.Graphics);
        config.ReadBuffer(ClusterItemsBufferId, GraphBufferUsage.Graphics);
        config.ReadBuffer(PointLightsBufferId, GraphBufferUsage.Graphics);
        config.ReadBuffer(DirectionalLightsBufferId, GraphBufferUsage.Graphics);
        config.ReadBuffer(EnvironmentVolumesBufferId, GraphBufferUsage.Graphics);
        config.ReadBuffer(LightBinsBufferId, GraphBufferUsage.Graphics);
        config.ReadBuffer(TileMasksBufferId, GraphBufferUsage.Graphics);
    }

    /// <summary>
    ///     Declares a read of every environment cubemap in the scene, which also orders their queued uploads ahead of
    ///     the pass.
    /// </summary>
    public void ReadEnvironment(IGraphConfig config)
    {
        var environments = Snapshot.Environments;
        for (var i = 0; i < environments.Length; i++)
            if (!IsRepeatedMap(environments, i))
                ReadCubemap(config, environments[i].Map.Cubemap);

        if (Environment.ReflectionLut.Id == 0) return;
        var lut = config.AddExternalImage(Environment.ReflectionLut);
        if (lut != 0) config.ReadTexture(lut, ImageLayout.ShaderReadOnly);
    }

    private static bool IsRepeatedMap(ReadOnlySpan<EnvironmentVolume> environments, int index)
    {
        for (var earlier = 0; earlier < index; earlier++)
            if (ReferenceEquals(environments[earlier].Map, environments[index].Map))
                return true;
        return false;
    }

    private static void ReadCubemap(IGraphConfig config, in ResourceHandle cubemap)
    {
        if (cubemap.Id == 0) return;
        var id = config.AddExternalImage(cubemap);
        if (id != 0) config.ReadCubemap(id, ImageLayout.ShaderReadOnly);
    }

    public WorldInfo CreateWorldInfo(ICompiledGraph graph)
    {
        return new WorldInfo
        {
            View = View,
            Projection = Projection,
            CameraPosition = ViewTransform.Position,
            Time = Time,
            Grid = graph.GetBufferOrException(LightGridBufferId).ToDeviceSpan<LightGridInfo>(),
            Channel = Channel,
            Environment = Environment.Info
        };
    }

    /// <summary>
    ///     Lets go of the snapshot, once this view's frame has rendered or been dropped.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Snapshot.Release();
    }

    public void Write(IGraphBuilder builder)
    {
        SceneFrame = Snapshot.Write(builder);

        builder.AddPass(new InitViewResourcesPass(this));
        var useIndirect = UseIndirectDrawing(DrawMode, IGraphicsModule.Get().CurrentDevice.SupportsIndirectRendering);
        foreach (var pass in CreateGeometryPasses(useIndirect)) builder.AddPass(pass);
        if (ToneMaps) builder.AddPass(new ToneMapPass(this));
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
    ///     The passes that cull, build draw commands, write depth, build the light grid, draw the lit scene into the
    ///     output image and then the sky behind it. The direct set has no culling or command passes.
    /// </summary>
    public IReadOnlyList<IPass> CreateGeometryPasses(bool useIndirect)
    {
        List<IPass> passes;
        if (useIndirect)
        {
            var cullingPass = new CullingPass(this);
            passes =
            [
                cullingPass,
                new FillIndirectBuffersPass(cullingPass, this),
                new DepthPrepassIndirectPass(this),
                new LightGridPass(this),
                new ForwardIndirectPass(this)
            ];
        }
        else
        {
            passes = [new DepthPrepassDirectPass(this), new LightGridPass(this), new ForwardDirectPass(this)];
        }

        if (DrawsSky) passes.Add(new SkyPass(this));
        return passes;
    }

    public bool DrawsSky => Environment.HasCubemap && Channel == RenderChannel.Scene;
}
