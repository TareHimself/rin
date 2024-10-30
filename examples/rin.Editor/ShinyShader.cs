using rin.Core;
using rin.Graphics;
using rin.Graphics.Material;
using rin.Widgets;
using rin.Widgets.Graphics;

namespace rin.Editor;

public class ShinyShader : Widget
{
    private readonly MaterialInstance _materialInstance;

    public ShinyShader()
    {
        var gs = SRuntime.Get().GetModule<SGraphicsModule>();

       // _materialInstance = SWidgetsModule.CreateMaterial(Path.Join(SRuntime.ShadersDir,"pretty.ash"));
    }   

    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
        _materialInstance.Dispose();
        ;
    }

    protected override void OnAddedToSurface(Surface surface)
    {
        base.OnAddedToSurface(surface);
        _materialInstance.BindBuffer("ui", surface.GlobalBuffer);
    }

    protected override void OnRemovedFromSurface(Surface surface)
    {
        base.OnRemovedFromSurface(surface);
    }

    protected override Size2d ComputeDesiredContentSize()
    {
        return new Size2d(400, 400);
    }

    public override void Collect(WidgetFrame frame, TransformInfo info)
    {
        frame.AddMaterialRect(_materialInstance, info);
    }
}