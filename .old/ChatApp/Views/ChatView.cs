using System.Numerics;
using Rin.Framework.Views;
using Rin.Framework.Views.Composite;
using Rin.Framework.Views.Content;
using Rin.Framework.Views.Graphics.Quads;
using Rin.Framework.Views.Layouts;

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
        Child = new TextBoxView
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
        Slots =
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
                    Child = new FlexBoxView
                    {
                        Axis = Axis.Row,
                        Slots =
                        [
                            new FlexBoxSlot
                            {
                                Child = new RectView
                                {
                                    Color = Color.Green,
                                    Child = new PanelView
                                    {
                                        Slots =
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
                                    Child = new RectView
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