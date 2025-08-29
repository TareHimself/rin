using System.Numerics;
using Rin.Framework;
using Rin.Framework.Audio;
using Rin.Framework.Graphics;
using Rin.Framework.Math;
using Rin.Framework.Views;
using Rin.Framework.Views.Animation;
using Rin.Framework.Views.Composite;
using Rin.Framework.Views.Content;
using Rin.Framework.Views.Events;
using Rin.Framework.Views.Graphics;
using Rin.Framework.Views.Layouts;
using YoutubeExplode.Common;


namespace rin.Examples.AudioPlayer.Views;

public class TrackPlayer : Overlay
{
    private readonly Panel _backgroundContainer = new();

    private readonly TextBox _currentTimeText = new()
    {
        Content = "00:00",
        FontSize = 30,
        Padding = new Padding
        {
            Top = 5.0f,
            Bottom = 5.0f,
            Right = 5.0f
        }
    };

    private readonly TextBox _endTimeText = new()
    {
        Content = "00:00",
        FontSize = 30,
        Padding = new Padding
        {
            Top = 5.0f,
            Bottom = 5.0f,
            Left = 5.0f
        }
    };

    private readonly TextBox _nameText = new()
    {
        Content = "NAME",
        FontSize = 40,
        FontFamily = "Noto Sans JP",
        Padding = new Padding
        {
            Top = 0.0f,
            Bottom = 5.0f
        },
        WrapContent = true
    };

    private readonly IChannel _stream;
    private double _lastTime = SApplication.Get().GetTimeSeconds();

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
                        Child = _nameText,
                        Fit = CrossFit.Fill
                    },
                    new ListSlot
                    {
                        Fit = CrossFit.Fill,
                        Child = new Constraint
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
                                            Child = new ProgressBar(() => (float)(_stream.Position / _stream.Duration),onClick:
                                                (progress) =>
                                                {
                                                    _stream.SetPosition(progress * _stream.Duration);
                                                })
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


        _endTimeText.Content = FormatTime(stream.Duration);
        Padding = 10.0f;
        FetchCover().ConfigureAwait(false);
        Pivot = new Vector2(1.0f, 0.0f);
    }

    private bool Loaded => Pivot.X <= 0.0f;

    public string Name
    {
        get => _nameText.Content;
        set => _nameText.Content = value;
    }

    private async Task FetchCover()
    {
        try
        {
            var data = (await SAudioPlayer.Get().YtClient.Search.GetVideosAsync($"{_nameText.Content} official track"))
                .FirstOrDefault();
            if (data == null)
            {
                Console.WriteLine($"Failed to find art for {_nameText.Content}");
                return;
            }


            var thumb = data.Thumbnails.MaxBy(c => c.Resolution.Width * c.Resolution.Height)!.Url;

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

    public override void Collect(in Matrix4x4 transform, in Rect2D clip, CommandList commands)
    {
        _currentTimeText.Content = FormatTime(_stream.Position);
        base.Collect(transform, clip, commands);
    }

    public override bool OnCursorDown(CursorDownSurfaceEvent e)
    {
        return true;
    }

    public override void OnCursorUp(CursorUpSurfaceEvent e)
    {
        if (_stream.IsPlaying)
            _stream.Pause();
        else
            _stream.Play();

        base.OnCursorUp(e);
    }

    protected override void OnCursorEnter(CursorMoveSurfaceEvent e)
    {
        base.OnCursorEnter(e);
        if (Loaded)
            this.TranslateTo(new Vector2(40.0f, 0.0f),
                easingFunction: EasingFunctions.EaseInOutCubic);
    }

    protected override void OnCursorLeave()
    {
        base.OnCursorLeave();
        if (Loaded)
            this.TranslateTo(new Vector2(0.0f, 0.0f),
                easingFunction: EasingFunctions.EaseInOutCubic);
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return base.LayoutContent(availableSpace);
    }
}