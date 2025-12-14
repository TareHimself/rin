using System.Numerics;
using rin.Examples.Common.Views;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Windows;
using Rin.Framework.Shared.Math;
using Rin.Framework.Views;
using Rin.Framework.Views.Composite;
using Rin.Framework.Views.Enums;
using Rin.Framework.Views.Events;
using Rin.Framework.Views.Graphics;
using Rin.Framework.Views.Graphics.Quads;
using Rin.Framework.Views.Layouts;

namespace NodeGraphTest;

public class GraphView : MultiSlotCompositeView<GraphSlot>, IGraphView
{
    public record GraphConnectionKey
    {
        public readonly IGraphPinView InputPin;
        public readonly IGraphPinView OutputPin;

        public GraphConnectionKey(IGraphPinView a, IGraphPinView b)
        {
            if (a.PinType == PinType.Input)
            {
                InputPin = a;
                OutputPin = b;
            }
            else
            {
                InputPin = b;
                OutputPin = a;
            }
        }
    }

    public class NodeDragInfo
    {
        public required IGraphNodeView NodeView { get; set; }
        public required Vector2 Offset { get; set; }
        
        public required GraphSlot Slot { get; set; }
    }
    
    // Input to output pin connections
    private readonly Dictionary<IGraphPinView,IGraphPinView> _connections = [];
    private readonly GraphLayout _layout;
    public GraphView()
    {
        _layout = new GraphLayout(this);
        
        Slots =
        [
            new GraphSlot()
            {
                Child = new GraphNodeTestView(),
                Position = new Vector2(100.0f, 100.0f)
            },
            new GraphSlot()
            {
                Child = new GraphNodeTestView(),
                Position = new Vector2(500.0f, 100.0f)
            }
        ];
    }

    private DefaultPinConnectionRequest? _pinConnectionRequest;
    public void StartPinDrag(CursorDownSurfaceEvent e, IGraphPinView pin, in Vector2 pinCenter)
    {
        e.Target = this;
        _pinConnectionRequest = new DefaultPinConnectionRequest(pin, pinCenter);
    }

    public void StartNodeDragging(CursorDownSurfaceEvent e, IGraphNodeView node)
    {
        e.Target = this;
        var localPosition = e.Position.Transform(ComputeAbsoluteContentTransform().Inverse());
        var slot = GetSlots().First(c => c.Child == node) as GraphSlot ?? throw new NullReferenceException();
        
        _currentDragInfo = new NodeDragInfo
        {
            NodeView = node,
            Offset = (slot.Position - localPosition),
            Slot = slot,
        };
    }

    public override IEnumerable<ISlot> GetSlots() => _layout.GetSlots();


   //  private Matrix4x4 ScrollOffset => Matrix4x4.Identity.Translate((GetContentSize() / 2));
   private Matrix4x4 ScrollOffset => Matrix4x4.Identity;
    public override Matrix4x4 GetLocalContentTransform()
    {
        return base.GetLocalContentTransform();
    }

    public override void Collect(in Matrix4x4 transform, in Rect2D clip, CommandList commands)
    {
        var offset = 50;
        foreach (var (input,output) in _connections)
        {
            commands.AddCubicCurve(Matrix4x4.Identity,input.GetPinAbsolutePosition(),output.GetPinAbsolutePosition(),input.GetPinAbsolutePosition() + new Vector2(-offset,0),output.GetPinAbsolutePosition() + new Vector2(offset,0),5,Color.Green);
        }
        
        {
            if (_pinConnectionRequest is { } request)
            {
                var requesterIsInput = request.Requester.PinType == PinType.Input;
                //commands.AddCubicCurve(Matrix4x4.Identity,request.From,request.Location,request.From + new Vector2(50.0f,0),request.Location - new Vector2(50.0f,0),4,Color.Blue);
                commands.AddCubicCurve(Matrix4x4.Identity,request.Requester.GetPinAbsolutePosition(),request.Position,request.Requester.GetPinAbsolutePosition() + new Vector2((requesterIsInput ? -offset : offset),0),request.Position + new Vector2((!requesterIsInput ? -offset : offset),0),5,Color.Green);
            }
        }
        base.Collect(in transform, in clip, commands);
    }

    // protected override Matrix4x4 ComputeSlotTransform(ISlot slot, in Matrix4x4 contentTransform)
    // {
    //     var size = GetSize() / 2;
    //     return base.ComputeSlotTransform(slot,Matrix4x4.Identity.Translate(size).ChildOf(contentTransform));
    // }
    
    protected override Vector2 ArrangeContent(in Vector2 availableSpace)
    {
        var contentSize = GetContentSize();
        var computedContentSize = _layout.Apply(availableSpace);
        var delta = computedContentSize - contentSize;
        return computedContentSize;
    }

    public override void OnChildInvalidated(IView child, InvalidationType invalidation)
    {
        if (_layout.FindSlot(child) is GraphSlot slot)
        {
            _layout.OnSlotUpdated(slot);
        }
    }

    public override void OnCursorMove(CursorMoveSurfaceEvent e, in Matrix4x4 transform)
    {
        if (_pinConnectionRequest is { } request)
        {
            request.Position = e.Position;
        }

        if (_currentDragInfo is { } dragInfo)
        {
            var localSpace = e.Position.Transform(transform.Inverse());
            dragInfo.Slot.Position = dragInfo.Offset + localSpace;
            dragInfo.Slot.Apply();
        }
    }

    public override Vector2 ComputeDesiredContentSize() => _layout.ComputeDesiredContentSize();

    public override void OnCursorUp(CursorUpSurfaceEvent e)
    {
        base.OnCursorUp(e);
        if (_pinConnectionRequest is { } request && Surface is not null)
        {
            request.Position = e.Position;
            var connectionEvent = new PinConnectAttemptEvent(Surface, request);
            HandleEvent(connectionEvent,ComputeAbsoluteTransform());
            if (connectionEvent.Handled)
            {
                IGraphPinView input;
                IGraphPinView output;
                
                if (request.Requester.PinType == PinType.Input)
                {
                    input = request.Requester;
                    output = connectionEvent.PinView;
                }
                else
                {
                    input = connectionEvent.PinView;
                    output = request.Requester;
                }

                if (_connections.ContainsKey(input))
                {
                    _connections.Remove(input);
                }
                
                _connections.Add(input,output);
            }
        }
        _pinConnectionRequest = null;
        _currentDragInfo = null;
    }

    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        if (e.Button == CursorButton.Two)
        {
            var localPosition = e.Position.Transform(ScrollOffset.ChildOf(transform).Inverse());
            Add(new GraphSlot()
            {
                Child = new GraphNodeTestView(),
                Position = localPosition
            });
            e.Target = this;
        }
        base.OnCursorDown(e, in transform);
    }

    private NodeDragInfo? _currentDragInfo = null;

    public override int SlotCount => _layout.SlotCount;
    public override bool Add(IView child) => _layout.Add(child);

    public override bool Add(GraphSlot slot) => _layout.Add(slot);

    public override bool Remove(IView child) => _layout.Remove(child);
}