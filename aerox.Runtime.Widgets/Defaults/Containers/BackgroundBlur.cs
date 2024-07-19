using System.Runtime.InteropServices;
using aerox.Runtime.Graphics;
using aerox.Runtime.Graphics.Descriptors;
using aerox.Runtime.Graphics.Material;
using aerox.Runtime.Math;
using aerox.Runtime.Widgets.Graphics.Commands;

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
            Filter = ImageFilter.Linear,
            Tiling = ImageTiling.Repeat
        });
        _materialInstance.BindTo(frame);
        _materialInstance.BindBuffer("ui", frame.Surface.GlobalBuffer);
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
        _materialInstance = new MaterialInstance(Path.Join(SWidgetsModule.ShadersDir,"blur.ash"));
    }

    public BackgroundBlur()
    {
        _materialInstance = new MaterialInstance(Path.Join(SWidgetsModule.ShadersDir,"blur.ash"));
    }


    protected override Size2d ComputeDesiredSize()
    {
        return Slots.FirstOrDefault()?.GetWidget().GetDesiredSize() ?? new Size2d();
    }

    protected override void OnAddedToSurface(WidgetSurface widgetSurface)
    {
        base.OnAddedToSurface(widgetSurface);
        _materialInstance.BindBuffer("ui", widgetSurface.GlobalBuffer);
    }

    public override void Collect(WidgetFrame frame, DrawInfo info)
    {
        frame.AddCommands(new ReadBack(),new BlurCommand(_materialInstance, new BlurPushConstants
        {
            Transform = info.Transform,
            Size = GetDrawSize(),
            BlurRadius = 20.0f,
            Tint = Tint
        }));

        foreach (var slot in Slots.ToArray())
        {
            var slotDrawInfo = info.AccountFor(slot.GetWidget());
            if (slotDrawInfo.Occluded) continue;
            slot.GetWidget().Collect(frame, info.AccountFor(slot.GetWidget()));
        }
    }

    protected override void ArrangeSlots(Size2d drawSize)
    {
        var widget = Slots.FirstOrDefault()?.GetWidget();
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