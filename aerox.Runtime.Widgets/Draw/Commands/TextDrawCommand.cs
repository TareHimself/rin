using aerox.Runtime.Graphics.Material;
using aerox.Runtime.Widgets.Defaults.Content;

namespace aerox.Runtime.Widgets.Draw.Commands;

public class TextDrawCommand : DrawCommand
{
    private readonly MtsdfFont _font;
    private readonly MaterialInstance _materialInstance;
    private readonly TextPushConstants[] _pushConstants;

    public TextDrawCommand(MaterialInstance materialInstance, MtsdfFont font, TextPushConstants[] pushConstants)
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
    

    protected override void Draw(WidgetFrame frame)
    {
        _materialInstance.BindTo(frame);
        foreach (var push in _pushConstants)
        {
            _materialInstance.Push(frame.Raw.GetCommandBuffer(),  push);
            Quad(frame);
        }
    }
}