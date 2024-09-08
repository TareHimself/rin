using aerox.Runtime;
using aerox.Runtime.Graphics;
using aerox.Runtime.Graphics.Material;
using aerox.Runtime.Widgets;

namespace WidgetTest;

public class LargeShader : Widget
{
    
    private readonly MaterialInstance _materialInstance;
    
    public LargeShader()
    {
        var gs = SRuntime.Get().GetModule<SGraphicsModule>();

        _materialInstance = new MaterialInstance(Path.Join(SWidgetsModule.ShadersDir,"pretty.ash"));
    }
    
    protected override void OnAddedToSurface(WidgetSurface widgetSurface)
    {
        base.OnAddedToSurface(widgetSurface);
        _materialInstance.BindBuffer("ui", widgetSurface.GlobalBuffer);
    }
    protected override Size2d ComputeDesiredSize() => new Size2d(0, 0);

    public override void Collect(WidgetFrame frame, DrawInfo info)
    {
        frame.AddMaterialRect(_materialInstance, new WidgetPushConstants()
        {
            Transform = this.ComputeRelativeTransform(),
            Size = this.GetDrawSize(),
        });
    }

    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
        _materialInstance.Dispose();
    }
}