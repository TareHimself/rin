using aerox.Runtime.Graphics.Material;
using aerox.Runtime.Widgets.Defaults.Content;

namespace aerox.Runtime.Widgets.Draw.Commands;

public class Text : Command
{
    private readonly MaterialInstance _materialInstance;
    private readonly MsdfFont _font;
    private readonly TextPushConstants[] _pushConstants;
    
    public Text(MaterialInstance materialInstance, MsdfFont font, TextPushConstants[] pushConstants) : base()
    {
        materialInstance.Reserve();
        font.Reserve();
        _materialInstance = materialInstance;
        _font = font;
        _pushConstants = pushConstants;
    }

    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
        _materialInstance.Dispose();
        _font.Dispose();
    }
    
    public override void Bind(WidgetFrame frame)
    {
        _materialInstance.BindTo(frame);
    }

    public override void Run(WidgetFrame frame)
    {
        foreach (var push in _pushConstants)
        {
            _materialInstance.Push(frame.Raw.GetCommandBuffer(),"pFont",push);
            CmdDrawQuad(frame);
        }
    }
    
    
}