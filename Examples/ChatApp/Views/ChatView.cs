using System.Numerics;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Graphics.Quads;
using Rin.Core.Views.Layouts;

namespace ChatApp.Views;

public class ChatView : FlexBoxView
{
    private readonly ListView _chatItems = new();

    private TextInputBoxView _inputText = new()
    {
        FontSize = 20.0f
    };

    private ButtonView _sendButton = new()
    {
        Padding = 5.0f,
        BorderRadius = new Vector4(2.0f),
        InitChild = new TextBoxView
        {
            Content = "Send",
            FontSize = 20.0f
        },
        Color = Color.Green
    };

    public ChatView()
    {
        Padding = new Padding(20.0f);
        Axis = Axis.Column;
        InitSlots =
        [
            new FlexBoxSlot
            {
                Child = new CanvasView
                {
                    Paint = (self, transform, cmds) => { cmds.AddText(transform, "Noto Sans", "Hello World", 60.0f); }
                }, //_chatItems,
                Fit = CrossFit.Fill,
                Flex = 1
            },
            new FlexBoxSlot
            {
                Child = new SizerView
                {
                    HeightOverride = 50,
                    InitChild = new FlexBoxView
                    {
                        Axis = Axis.Row,
                        InitSlots =
                        [
                            new FlexBoxSlot
                            {
                                Child = new RectView
                                {
                                    Color = Color.Green,
                                    InitChild = new PanelView
                                    {
                                        InitSlots =
                                        [
                                            new PanelSlot
                                            {
                                                Child = new TextInputBoxView
                                                {
                                                    FontSize = 20.0f
                                                },
                                                MaxAnchor = Vector2.One
                                            }
                                        ]
                                    }
                                },
                                Flex = 1,
                                Fit = CrossFit.Fill
                            },
                            new FlexBoxSlot
                            {
                                Child = new SizerView
                                {
                                    InitChild = new RectView
                                    {
                                        Color = Color.Red
                                    },
                                    WidthOverride = 50
                                },
                                Fit = CrossFit.Fill
                            }
                        ]
                    }
                },
                Fit = CrossFit.Fill
            }
        ];
    }
}