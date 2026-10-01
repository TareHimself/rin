using Rin.Core.Graphics;
using Rin.World.Components;
using Rin.World.Graphics.Default;
using Rin.World.Graphics.Default.Passes;

namespace Rin.World.Tests.Graphics;

public class DefaultWorldViewDataTests
{
    private static DefaultWorldViewData CreateView(DefaultRenderSystem render)
    {
        return (DefaultWorldViewData)render.Snapshot(new CameraComponent(), new Extent2D(1, 1));
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
