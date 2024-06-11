using aerox.Runtime;
using aerox.Runtime.Graphics;
using aerox.Runtime.Graphics.Material;
using aerox.Runtime.Widgets;

namespace aerox.Editor;

public class ShinyShader : Widget
{
    private readonly MaterialInstance _materialInstance;

    public ShinyShader()
    {
        var gs = SRuntime.Get().GetModule<SGraphicsModule>();

        _materialInstance = SWidgetsModule.CreateMaterial(Path.Join(SRuntime.ShadersDir,"pretty.ash"));
    }   

    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
        _materialInstance.Dispose();
        ;
    }

    protected override void OnAddedToRoot(WidgetSurface widgetSurface)
    {
        base.OnAddedToRoot(widgetSurface);
        _materialInstance.BindBuffer("ui", widgetSurface.GlobalBuffer);
    }

    protected override void OnRemovedFromRoot(WidgetSurface widgetSurface)
    {
        base.OnRemovedFromRoot(widgetSurface);
    }

    public override Size2d ComputeDesiredSize()
    {
        return new Size2d(400, 400);
    }

    public override void Draw(WidgetFrame frame, DrawInfo info)
    {
        frame.AddMaterialRect(_materialInstance, info);
    }
}