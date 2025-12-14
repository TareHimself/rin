using System.Numerics;
using Rin.Framework.Graphics;
using Rin.Framework.Views;
using Rin.Framework.Views.Composite;
using Rin.Framework.Views.Content;
using Rin.Framework.Views.Graphics;
using Rin.Framework.Views.Graphics.Quads;

namespace NodeGraphTest;

public class TextPinView : PinTestView
{
    public TextPinView(string name)
    {
        Name = name;
        Child = new TextBoxView
        {
            Content = name,
            FontSize = 15.0f
        };
    }


    public override bool TryHandleConnectionRequest(IPinConnectionRequest request)
    {
        if (request.Requester.PinType != PinType && request.Requester is TextPinView)
        {
            return true;
        }
        return base.TryHandleConnectionRequest(request);
    }

    public override void Collect(in Matrix4x4 transform, in Rect2D clip, CommandList commands)
    {
        
        base.Collect(in transform, in clip, commands);
        
    }
}