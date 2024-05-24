using aerox.Runtime.Graphics.Material;

namespace aerox.Runtime.Widgets.Draw.Commands;

public class MaterialRect : Command
{
    private readonly MaterialInstance _materialInstance;
    private readonly WidgetPushConstants _pushConstants;
    public MaterialRect(MaterialInstance materialInstance,WidgetPushConstants pushConstant) : base()
    {
        materialInstance.Reserve();
        _materialInstance = materialInstance;
        _pushConstants = pushConstant;
    }

    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
        _materialInstance.Dispose();
    }
    
    public override void Bind(WidgetFrame frame)
    {
        _materialInstance.BindTo(frame);
    }
    
    public override void Run(WidgetFrame frame)
    {
        _materialInstance.Push(frame.Raw.GetCommandBuffer(), "pRect", _pushConstants);
        CmdDrawQuad(frame);
    }
}