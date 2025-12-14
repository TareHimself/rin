using System.Numerics;
using System.Text.Json.Nodes;
using Rin.Framework.Graphics;
using Rin.Framework.Shared.Math;
using Rin.Framework.Views;
using Rin.Framework.Views.Composite;
using Rin.Framework.Views.Enums;
using Rin.Framework.Views.Events;
using Rin.Framework.Views.Graphics;
using Rin.Framework.Views.Graphics.Quads;
using Rin.Framework.Views.Layouts;

namespace NodeGraphTest;

public class PinTestView : SingleSlotCompositeView, IGraphPinView
{
    public PinTestView()
    {
        Clip = Clip.None;
        Padding = new Padding(0,5);
    }
    public PinType PinType { get; set; }
    private float _contentXSize = 0.0f;
    protected override Vector2 ArrangeContent(in Vector2 availableSpace)
    {
        var pinSize = NodeGraphConstants.PinDiameterWithPadding;
        var pinHalfSize = pinSize / 2f;
        if (GetSlot()?.Child is { } view)
        {
            var contentSize = view.ComputeSize(availableSpace with{ X = float.IsFinite(availableSpace.X) ? availableSpace.X - pinSize : availableSpace.X });
            _contentXSize = contentSize.X;
            var ySize = float.Max(contentSize.Y,pinSize);
            view.Offset = new Vector2(PinType == PinType.Input ? pinSize : 0,(ySize - contentSize.Y) / 2f); 
            return contentSize with { X = contentSize.X + pinSize,Y = ySize };
        }
        
        return new Vector2(pinHalfSize);
    }

    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        var absMatrix = Matrix4x4.Identity.Translate(GetPinOffset()).ChildOf(ComputeAbsoluteContentTransform());
        var rectSize = new Vector2(NodeGraphConstants.PinDiameterWithPadding);
        if (Rect2D.PointWithin(rectSize, absMatrix, e.Position))
        {
            ParentNode?.StartPinDrag(e,this,new Vector2(NodeGraphConstants.PinRadius).Transform(absMatrix));
        }
    }
    
    public override void HandleCustomEvent(ISurfaceEvent e, in Matrix4x4 transform)
    {
        if (e is IPinConnectionAttemptEvent { Handled: false } attempt)
        {
            if (TryHandleConnectionRequest(attempt.Request))
            {
                attempt.PinView = this;
            }
        }
    }

    protected Vector2 GetPinOffset()
    {
        var contentSize = GetContentSize();
        var pinSize = new Vector2(NodeGraphConstants.PinDiameter);
        var sizeWithPadding = pinSize + new Vector2(NodeGraphConstants.PinPadding * 2f);
        var offsetX = PinType == PinType.Input ? NodeGraphConstants.PinPadding : _contentXSize + NodeGraphConstants.PinPadding;
        var offsetY = (contentSize.Y / 2 - sizeWithPadding.Y / 2) + NodeGraphConstants.PinPadding;
        return new Vector2(offsetX, offsetY);
    }


    private Vector2 _pinPosition = Vector2.Zero;
    
    public Vector2 GetPinAbsolutePosition()
    {
        return _pinPosition;
    }
    

    protected void CollectPin(in Matrix4x4 transform,CommandList commands)
    {
        var offset = GetPinOffset();
        var pinTransform = transform.Translate(offset);
        _pinPosition = new Vector2(0).Transform(Matrix4x4.Identity.Translate(GetPinOffset() + new Vector2(NodeGraphConstants.PinRadius)).ChildOf(transform));
        // commands.AddRect(
        //     Matrix4x4.Identity.Translate(offset with { X = offset.X - NodeGraphConstants.PinPadding,Y = offset.Y - NodeGraphConstants.PinPadding })
        //         .ChildOf(transform), new Vector2(NodeGraphConstants.PinDiameterWithPadding), Color.Green);
        commands.AddCircle(pinTransform, NodeGraphConstants.PinRadius,Color.Green);
        //commands.AddRect(pinTransform,new Vector2(NodeGraphConstants.PinRadius * 2f),Color.Red);
    }
    public override void Collect(in Matrix4x4 transform, in Rect2D clip, CommandList commands)
    {
        var contentTransform = Matrix4x4.Identity.Translate(new Vector2(Padding.Left, Padding.Top)).ChildOf(transform);
        //commands.AddRect(contentTransform,GetContentSize(),Color.Green);
        base.Collect(in transform, in clip, commands);
        CollectPin(contentTransform, commands);
    }

    public virtual bool TryHandleConnectionRequest(IPinConnectionRequest request)
    {
        return false;
    } 

    public void JsonSerialize(JsonObject output)
    {
        throw new NotImplementedException();
    }

    public void JsonDeserialize(JsonObject input)
    {
        throw new NotImplementedException();
    }

    public string Name { get; set; } = string.Empty;
    public IGraphNodeView? ParentNode { get; set; }
}