using System.Numerics;
using System.Text.Json.Nodes;
using Rin.Framework.Graphics;
using Rin.Framework.Views;
using Rin.Framework.Views.Composite;
using Rin.Framework.Views.Content;
using Rin.Framework.Views.Events;
using Rin.Framework.Views.Graphics;
using Rin.Framework.Views.Graphics.Quads;
using Rin.Framework.Views.Layouts;

namespace NodeGraphTest;

public class GraphNodeTestView : ListView, IGraphNodeView
{
    private PinTestView[] _inputPins =
    [
        new TextPinView("Input A")
        {
            PinType = PinType.Input
        },
        new TextPinView("Input B")
        {
            PinType = PinType.Input
        },
        new TextPinView("Input C")
        {
            PinType = PinType.Input
        }
    ];

    private PinTestView[] _outputPins =
    [
        new TextPinView("Result")
        {
            PinType = PinType.Output
        }
    ];

    public GraphNodeTestView()
    {
        Axis = Axis.Row;
        Children =
        [
            new ListView
            {
                Axis = Axis.Column,
                Slots =
                [
                    new ListSlot
                    {
                        Child = new TextBoxView
                        {
                            Content = "Test Node",
                            FontSize = 20,
                            Padding = new Padding(40, 5),
                        }
                    },
                    .._outputPins.Select(c => new ListSlot
                    {
                        Child = c,
                        Align = CrossAlign.End,
                    }),
                    .._inputPins.Select(c => new ListSlot
                    {
                        Child = c
                    }),
                ],
                Padding = new Padding(0,10)
            }
        ];
        Padding = new Padding(0, 0);
        foreach (var pin in InputPins)
        {
            pin.ParentNode = this;
        }

        foreach (var pin in OutputPins)
        {
            pin.ParentNode = this;
        }
    }

    public override void Collect(in Matrix4x4 transform, in Rect2D clip, CommandList commands)
    {
        var halfPinSpace = NodeGraphConstants.PinPadding + NodeGraphConstants.PinRadius;
        // commands.AddRect(Matrix4x4.Identity.Translate(new Vector2(halfPinSpace,0)).ChildOf(transform), GetContentSize() - new Vector2(halfPinSpace * 2,0),new Color(0.19f,1), new Vector4(20f));
        commands.AddRect(transform, GetContentSize(),new Color(0.19f,1), new Vector4(20f));
        //commands.AddRect(transform, GetContentSize(), Color.Blue, new Vector4(5f));
        base.Collect(in transform, in clip, commands);
    }

    public void JsonSerialize(JsonObject output)
    {
        throw new NotImplementedException();
    }

    public void JsonDeserialize(JsonObject input)
    {
        throw new NotImplementedException();
    }

    public string InstanceId { get; set; } = string.Empty;
    public IEnumerable<IGraphPinView> InputPins => _inputPins;
    public IEnumerable<IGraphPinView> OutputPins => _outputPins;


    public void StartPinDrag(CursorDownSurfaceEvent e, IGraphPinView pin, in Vector2 pinCenter)
    {
        if (Parent is IGraphView view)
        {
            view.StartPinDrag(e, pin, pinCenter);
        }
    }

    public void TryConnectPin(IPinConnectionRequest request)
    {
        throw new NotImplementedException();
    }

    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        if (Parent is IGraphView view)
        {
            view.StartNodeDragging(e,this);
        }
        base.OnCursorDown(e,transform);
    }
}