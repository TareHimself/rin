using System.Numerics;
using Rin.Framework.Views;
using Rin.Framework.Views.Composite;
using Rin.Framework.Views.Content;
using Rin.Framework.Views.Graphics.Quads;
using Rin.Framework.Views.Layouts;
using Rect = Rin.Framework.Views.Composite.Rect;

namespace ChatApp.Views;

public class ChatView : FlexBox
{
    private readonly List _chatItems = new();

    private TextInputBox _inputText = new()
    {
        FontSize = 20.0f
    };

    private Button _sendButton = new()
    {
        Padding = 5.0f,
        BorderRadius = new Vector4(2.0f),
        Child = new TextBox
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
                Child = new Canvas
                {
                    Paint = (self, transform, cmds) => { cmds.AddText(transform, "Noto Sans", "Hello World", 60.0f); }
                }, //_chatItems,
                Fit = CrossFit.Fill,
                Flex = 1
            },
            new FlexBoxSlot
            {
                Child = new Sizer
                {
                    HeightOverride = 50,
                    Child = new FlexBox
                    {
                        Axis = Axis.Row,
                        Slots =
                        [
                            new FlexBoxSlot
                            {
                                Child = new Rect
                                {
                                    Color = Color.Green,
                                    Child = new Panel
                                    {
                                        Slots =
                                        [
                                            new PanelSlot
                                            {
                                                Child = new TextInputBox
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
                                Child = new Sizer
                                {
                                    Child = new Rect
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