using System.Numerics;
using Rin.Engine.Audio;
using Rin.Engine.Core;
using Rin.Engine.Core.Math;
using Rin.Engine.Views;
using Rin.Engine.Views.Animation;
using Rin.Engine.Views.Composite;
using Rin.Engine.Views.Content;
using Rin.Engine.Views.Events;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Layouts;
using Rect = Rin.Engine.Views.Rect;


namespace rin.Examples.AudioPlayer.Views;

public class TrackPlayer : Overlay
{
    private readonly TextBox _nameText = new TextBox
    {
        Content = "NAME",
        FontSize = 40,
        FontFamily = "Noto Sans JP",
        Padding = new Padding()
        {
            Top = 0.0f,
            Bottom = 5.0f,
        },
        WrapContent = true
    };

    private readonly TextBox _currentTimeText = new TextBox
    {
        Content = "00:00",
        FontSize = 30,
        Padding = new Padding()
        {
            Top = 5.0f,
            Bottom = 5.0f,
            Right = 5.0f
        }
    };

    private readonly TextBox _endTimeText = new TextBox
    {
        Content = "00:00",
        FontSize = 30,
        Padding = new Padding()
        {
            Top = 5.0f,
            Bottom = 5.0f,
            Left = 5.0f
        }
    };

    private readonly IChannel _stream;
    private double _lastTime = SEngine.Get().GetTimeSeconds();

    private readonly Panel _backgroundContainer = new Panel();

    private bool Loaded => Pivot.X <= 0.0f;

    public string Name
    {
        get => _nameText.Content;
        set => _nameText.Content = value;
    }

    public TrackPlayer(string name, IChannel stream)
    {
        Visibility = Visibility.Hidden;
        _nameText.Content = name;

        _stream = stream;

        Children =
        [
            _backgroundContainer,
            new List
            {
                Axis = Axis.Column,
                Padding = new Padding(5.0f, 10.0f),
                Slots =
                [
                    new ListSlot
                    {
                        Child = _nameText
                    },
                    new ListSlot
                    {
                        Fit = CrossFit.Fill,
                        Child = new Constraint()
                        {
                            MinWidth = 500.0f,
                            Child = new FlexBox
                            {
                                Axis = Axis.Row,
                                Slots =
                                [
                                    new FlexBoxSlot
                                    {
                                        Child = new Sizer
                                        {
                                            Child = _currentTimeText,
                                            WidthOverride = 100
                                        }
                                    },
                                    new FlexBoxSlot
                                    {
                                        Child = new Sizer
                                        {
                                            Child = new ProgressBar(() => (float)(_stream.Position / _stream.Length))
                                            {
                                                BackgroundColor = Color.Black,
                                                BorderRadius = new Vector4(6.0f)
                                            },
                                            HeightOverride = 8
                                        },
                                        Flex = 1,
                                        Align = CrossAlign.Center
                                    },
                                    new FlexBoxSlot
                                    {
                                        Child = new Sizer
                                        {
                                            Child = _endTimeText,
                                            WidthOverride = 100
                                        }
                                    }
                                ]
                            }
                        }
                    }
                ]
            }
        ];


        _endTimeText.Content = FormatTime(stream.Length);
        Padding = 10.0f;
        FetchCover().ConfigureAwait(false);
        Pivot = new Vector2(1.0f, 0.0f);
    }

    private async Task FetchCover()
    {
        try
        {
            var data = (await SAudioPlayer.Get().SpClient.Search.GetTracksAsync($"{_nameText.Content} official track"))
                .FirstOrDefault();
            if (data == null)
            {
                Console.WriteLine($"Failed to find art for {_nameText.Content}");
                return;
            }


            var thumb = data.Album.Images.MaxBy(c => c.Height * c.Width)!.Url;

            Console.WriteLine($"Using thumb {thumb} for {_nameText.Content}");
            _backgroundContainer.Add(new PanelSlot
            {
                Child = new TrackImage(thumb)
                {
                    BorderRadius = new Vector4(20.0f)
                },
                MinAnchor = new Vector2(0.0f),
                MaxAnchor = new Vector2(1.0f)
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            _backgroundContainer.Add(new PanelSlot
            {
                Child = new TrackImage("https://i.imgur.com/5fQUPDl.jpg")
                {
                    BorderRadius = new Vector4(20.0f)
                },
                MinAnchor = new Vector2(0.0f),
                MaxAnchor = new Vector2(1.0f)
            });
        }
        // this.ScaleTo(new Vector2<float>(1.0f, 1.0f), 1.0f, 0.2f,easingFunction: EasingFunctions.EaseInExpo).After().Do(
        //     () =>
        //     {
        //         _loaded = true;
        //     });
    }

    private static string FormatTime(double secs)
    {
        return
            $"{((int)Math.Floor(secs / 60)).ToString().PadLeft(2, '0')}:{((int)(secs % 60)).ToString().PadLeft(2, '0')}";
    }

    public override void Collect(Mat3 transform, Rect clip, PassCommands passCommands)
    {
        _currentTimeText.Content = FormatTime(_stream.Position);
        base.Collect(transform, clip, passCommands);
    }

    public override bool OnCursorDown(CursorDownSurfaceEvent e) => true;

    public override void OnCursorUp(CursorUpSurfaceEvent e)
    {
        if (_stream.IsPlaying)
        {
            _stream.Pause();
        }
        else
        {
            _stream.Play();
        }

        base.OnCursorUp(e);
    }

    protected override void OnCursorEnter(CursorMoveSurfaceEvent e)
    {
        base.OnCursorEnter(e);
        if (Loaded)
        {
            this.TranslateTo(new Vector2(40.0f, 0.0f), 0.2f,
                easingFunction: EasingFunctions.EaseInOutCubic);
        }
    }

    protected override void OnCursorLeave()
    {
        base.OnCursorLeave();
        if (Loaded)
        {
            this.TranslateTo(new Vector2(0.0f, 0.0f), 0.2f,
                easingFunction: EasingFunctions.EaseInOutCubic);
        }
    }

    protected override Vector2 LayoutContent(Vector2 availableSpace)
    {
        return base.LayoutContent(availableSpace);
    }
}