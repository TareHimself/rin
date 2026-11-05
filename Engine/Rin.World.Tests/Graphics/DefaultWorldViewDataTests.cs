using System.Numerics;
using Rin.Core.Graphics;
using Rin.World.Components;
using Rin.World.Graphics;
using Rin.World.Graphics.Default;
using Rin.World.Graphics.Default.Passes;

namespace Rin.World.Tests.Graphics;

public class DefaultWorldViewDataTests
{
    private static DefaultWorldViewData CreateView(DefaultRenderSystem render)
    {
        return (DefaultWorldViewData)render.Snapshot(new CameraComponent().ToCameraView(), new Extent2D(1, 1));
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
            typeof(FillGBufferIndirectPass)
        }));
    }

    [Test]
    public void DirectGeometryPassesHaveNoCullingOrCommandPasses()
    {
        var passes = CreateView(new DefaultRenderSystem()).CreateGeometryPasses(false);

        Assert.That(passes.Select(p => p.GetType()), Is.EqualTo(new[]
        {
            typeof(DepthPrepassDirectPass),
            typeof(FillGBufferDirectPass)
        }));
    }

    [Test]
    public void RenderSystemPassesItsDrawModeToTheViews()
    {
        var render = new DefaultRenderSystem { DrawMode = MeshDrawMode.Direct };

        Assert.That(CreateView(render).DrawMode, Is.EqualTo(MeshDrawMode.Direct));
    }
}
