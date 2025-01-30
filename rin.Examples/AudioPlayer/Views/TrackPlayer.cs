using rin.Framework.Audio;
using rin.Framework.Core;
using rin.Framework.Core.Math;
using rin.Framework.Views;
using rin.Framework.Views.Animation;
using rin.Framework.Views.Composite;
using rin.Framework.Views.Content;
using rin.Framework.Views.Events;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Layouts;
using Rect = rin.Framework.Views.Rect;


namespace rin.Examples.AudioPlayer.Views;

public class TrackPlayer : Overlay
{
    private readonly TextBox _nameText = new TextBox("NAME", 40)
    {
        Padding = new Padding()
        {
            Top = 0.0f,
            Bottom = 5.0f,
        },
        WrapContent = true
    };

    private readonly TextBox _currentTimeText = new TextBox("00:00", 30)
    {
        Padding = new Padding()
        {
            Top = 5.0f,
            Bottom = 5.0f,
            Right = 5.0f
        }
    };

    private readonly TextBox _endTimeText = new TextBox("00:00", 30)
    {
        Padding = new Padding()
        {
            Top = 5.0f,
            Bottom = 5.0f,
            Left = 5.0f
        }
    };

    private readonly IChannel _stream;
    private double _lastTime = SRuntime.Get().GetTimeSeconds();

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
                                                BorderRadius = 6.0f
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
        Pivot = new Vec2<float>(1.0f, 0.0f);
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
                    BorderRadius = 20.0f
                },
                MinAnchor = 0.0f,
                MaxAnchor = 1.0f
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            _backgroundContainer.Add(new PanelSlot
            {
                Child = new TrackImage("https://i.imgur.com/5fQUPDl.jpg")
                {
                    BorderRadius = 20.0f
                },
                MinAnchor = 0.0f,
                MaxAnchor = 1.0f
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
        base.Collect(transform,clip, passCommands);
    }

    public override bool OnCursorDown(CursorDownEvent e) => true;

    public override void OnCursorUp(CursorUpEvent e)
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

    protected override void OnCursorEnter(CursorMoveEvent e)
    {
        base.OnCursorEnter(e);
        if (Loaded)
        {
            this.TranslateTo(new Vec2<float>(40.0f, 0.0f), 0.2,
                easingFunction: EasingFunctions.EaseInOutCubic);
        }
    }

    protected override void OnCursorLeave(CursorMoveEvent e)
    {
        base.OnCursorLeave(e);
        if (Loaded)
        {
            this.TranslateTo(new Vec2<float>(0.0f, 0.0f), 0.2,
                easingFunction: EasingFunctions.EaseInOutCubic);
        }
    }

    protected override Vec2<float> LayoutContent(Vec2<float> availableSpace)
    {
        return base.LayoutContent(availableSpace);
    }
}