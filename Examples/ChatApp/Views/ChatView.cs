using System.Numerics;
using Rin.Engine.Views;
using Rin.Engine.Views.Composite;
using Rin.Engine.Views.Content;
using Rin.Engine.Views.Layouts;
using Rect = Rin.Engine.Views.Composite.Rect;

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
                Child = _chatItems,
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