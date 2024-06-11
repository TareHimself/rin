using System.Runtime.InteropServices;
using aerox.Runtime.Graphics;
using aerox.Runtime.Graphics.Descriptors;
using aerox.Runtime.Graphics.Material;
using aerox.Runtime.Math;
using aerox.Runtime.Widgets.Draw.Commands;

namespace aerox.Runtime.Widgets.Defaults.Containers;

[StructLayout(LayoutKind.Sequential)]
public struct BlurPushConstants
{
    public Matrix3 Transform;

    public Vector2<float> Size;

    public float BlurRadius;

    public Vector4<float> Tint;
}

internal class BlurCommand : DrawCommand
{
    private readonly MaterialInstance _materialInstance;
    private readonly BlurPushConstants _pushConstants;

    public BlurCommand(MaterialInstance materialInstance, BlurPushConstants pushConstants)
    {
        materialInstance.Reserve();
        _materialInstance = materialInstance;
        _pushConstants = pushConstants;
    }

    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
        _materialInstance.Dispose();
    }
    
    protected override void Draw(WidgetFrame frame)
    {
        
        _materialInstance.BindImage("SourceT", frame.Surface.GetCopyImage(), DescriptorSet.ImageType.Texture, new SamplerSpec
        {
            Filter = EImageFilter.Linear,
            Tiling = EImageTiling.Repeat
        });
        _materialInstance.BindTo(frame);
        _materialInstance.Push(frame.Raw.GetCommandBuffer(),  _pushConstants);
        Quad(frame);
    }
}

public class BackgroundBlur : Container
{
    private readonly MaterialInstance _materialInstance;
    public Color Tint = Color.White;

    public BackgroundBlur(Widget child) : base(child)
    {
        _materialInstance = SWidgetsModule.CreateMaterial(Path.Join(SWidgetsModule.ShadersDir,"blur.ash"));
    }

    public BackgroundBlur()
    {
        _materialInstance = SWidgetsModule.CreateMaterial(Path.Join(SWidgetsModule.ShadersDir,"blur.ash"));
    }


    public override Size2d ComputeDesiredSize()
    {
        return slots.FirstOrDefault()?.GetWidget().GetDesiredSize() ?? new Size2d();
    }

    protected override void OnAddedToRoot(WidgetSurface widgetSurface)
    {
        base.OnAddedToRoot(widgetSurface);
        _materialInstance.BindBuffer("ui", widgetSurface.GlobalBuffer);
    }

    public override void Draw(WidgetFrame frame, DrawInfo info)
    {
        frame.AddCommands(new ReadBack(),new BlurCommand(_materialInstance, new BlurPushConstants
        {
            Transform = info.Transform,
            Size = GetDrawSize(),
            BlurRadius = 20.0f,
            Tint = Tint
        }));

        foreach (var widget in slots.Select(slot => slot.GetWidget())) widget.Draw(frame, info.AccountFor(widget));
    }

    protected override void ArrangeSlots(Size2d drawSize)
    {
        var widget = slots.FirstOrDefault()?.GetWidget();
        widget?.SetRelativeOffset(0.0f);
        widget?.SetDrawSize(drawSize);
    }

    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
        _materialInstance.Dispose();
    }

    public override uint GetMaxSlots()
    {
        return 1;
    }
}