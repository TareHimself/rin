using System.Numerics;
using Rin.Core.Graphics;
using Rin.World.Graphics;
using Rin.World.Graphics.Default;
using Rin.World.Graphics.Default.Passes;
using Rin.World.Graphics.ImageBasedLighting;
using Rin.World.Tests.Graphics.ImageBasedLighting;

namespace Rin.World.Tests.Graphics;

public class DefaultWorldViewDataTests
{
    private static DefaultWorldViewData CreateView(DefaultRenderSystem render)
    {
        return (DefaultWorldViewData)render.Snapshot(TestCameras.Identity, new Extent2D(1, 1));
    }

    [Test]
    public void TheViewIsBuiltFromTheCameraViewWithoutItsScale()
    {
        var world = Matrix4x4.CreateScale(3f) * Matrix4x4.CreateTranslation(1, 2, 3);
        var camera = new CameraView(world, 70f, 0.1f, 200f);

        var view = (DefaultWorldViewData)new DefaultRenderSystem().Snapshot(camera, new Extent2D(1, 1));

        Assert.That(view.ViewTransform.Position, Is.EqualTo(new Vector3(1, 2, 3)));
        Assert.That(view.ViewTransform.Scale, Is.EqualTo(Vector3.One));
        Assert.That(view.View, Is.EqualTo(Matrix4x4.CreateTranslation(-1, -2, -3)));
        Assert.That((view.FieldOfView, view.NearClip, view.FarClip), Is.EqualTo((70f, 0.1f, 200f)));
    }

    [TestCase(MeshDrawMode.Auto, true, true)]
    [TestCase(MeshDrawMode.Auto, false, false)]
    [TestCase(MeshDrawMode.Indirect, false, true)]
    [TestCase(MeshDrawMode.Direct, true, false)]
    public void DrawModeResolvesAgainstTheDevice(MeshDrawMode mode, bool deviceSupportsIndirect, bool expected)
    {
        Assert.That(DefaultWorldViewData.UseIndirectDrawing(mode, deviceSupportsIndirect), Is.EqualTo(expected));
    }

    [Test]
    public void IndirectGeometryPassesCullBuildCommandsThenDraw()
    {
        var passes = CreateView(new DefaultRenderSystem()).CreateGeometryPasses(true);

        Assert.That(passes.Select(p => p.GetType()), Is.EqualTo(new[]
        {
            typeof(CullingPass),
            typeof(FillIndirectBuffersPass),
            typeof(DepthPrepassIndirectPass),
            typeof(LightGridPass),
            typeof(ForwardIndirectPass)
        }));
    }

    [Test]
    public void AViewWithAnEnvironmentDrawsTheSkyLast()
    {
        var render = new DefaultRenderSystem();
        render.CreateEnvironmentProxy(TestEnvironments.Unbounded(1, Vector3.One));

        var passes = CreateView(render).CreateGeometryPasses(false);

        Assert.That(passes[^1], Is.TypeOf<SkyPass>());
    }

    [Test]
    public void TheViewCarriesTheCameraExposureAndCurve()
    {
        var camera = TestCameras.Identity with { Exposure = 0.5f, ToneMapCurve = ToneMapCurve.Aces };

        var view = (DefaultWorldViewData)new DefaultRenderSystem().Snapshot(camera, new Extent2D(1, 1));

        Assert.That((view.Exposure, view.ToneMapCurve), Is.EqualTo((0.5f, ToneMapCurve.Aces)));
    }

    [Test]
    public void ACameraViewDefaultsToUnitExposureAndAgX()
    {
        var view = new CameraView(Matrix4x4.Identity, 90f, 0.1f, 100f);

        Assert.That((view.Exposure, view.ToneMapCurve), Is.EqualTo((1f, ToneMapCurve.AgX)));
    }

    [TestCase(RenderChannel.Scene, true)]
    [TestCase(RenderChannel.Normal, false)]
    [TestCase(RenderChannel.LightCount, false)]
    public void OnlyTheLitSceneIsToneMapped(RenderChannel channel, bool expected)
    {
        var camera = TestCameras.Identity with { Channel = channel };

        var view = (DefaultWorldViewData)new DefaultRenderSystem().Snapshot(camera, new Extent2D(1, 1));

        Assert.That(view.ToneMaps, Is.EqualTo(expected));
    }

    [Test]
    public void ADebugChannelViewLeavesTheSkyOut()
    {
        var render = new DefaultRenderSystem();
        render.CreateEnvironmentProxy(TestEnvironments.Unbounded(1, Vector3.One));
        var camera = TestCameras.Identity with { Channel = RenderChannel.Normal };

        var view = (DefaultWorldViewData)render.Snapshot(camera, new Extent2D(1, 1));

        Assert.That(view.DrawsSky, Is.False);
    }

    [Test]
    public void DirectGeometryPassesHaveNoCullingOrCommandPasses()
    {
        var passes = CreateView(new DefaultRenderSystem()).CreateGeometryPasses(false);

        Assert.That(passes.Select(p => p.GetType()), Is.EqualTo(new[]
        {
            typeof(DepthPrepassDirectPass),
            typeof(LightGridPass),
            typeof(ForwardDirectPass)
        }));
    }

    [Test]
    public void RenderSystemPassesItsDrawModeToTheViews()
    {
        var render = new DefaultRenderSystem { DrawMode = MeshDrawMode.Direct };

        Assert.That(CreateView(render).DrawMode, Is.EqualTo(MeshDrawMode.Direct));
    }

    [Test]
    public void TheViewDrawsTheChannelItsCameraViewAsksFor()
    {
        var camera = TestCameras.Identity with { Channel = RenderChannel.LightCount };

        var view = (DefaultWorldViewData)new DefaultRenderSystem().Snapshot(camera, new Extent2D(1, 1));

        Assert.That(view.Channel, Is.EqualTo(RenderChannel.LightCount));
    }
}
