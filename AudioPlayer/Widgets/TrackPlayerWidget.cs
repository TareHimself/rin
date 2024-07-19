using System.Net;
using aerox.Runtime.Audio;
using aerox.Runtime.Graphics;
using aerox.Runtime.Math;
using aerox.Runtime.Widgets;
using aerox.Runtime.Widgets.Defaults.Containers;
using aerox.Runtime.Widgets.Defaults.Content;
using aerox.Runtime.Widgets.Events;
using MathNet.Numerics;
using SixLabors.ImageSharp.PixelFormats;
using TerraFX.Interop.Vulkan;
using Image = SixLabors.ImageSharp.Image;

namespace AudioPlayer.Widgets;

public class TrackPlayerWidget : Overlay
{
    private Text NameText => GetChildSlot(1)!.GetWidget<BackgroundBlur>()!.GetChildSlot(0)!.GetWidget<List>()!.GetChildSlot(0)!.GetWidget<Text>()!;
    private Text StatusText => GetChildSlot(1)!.GetWidget<BackgroundBlur>()!.GetChildSlot(0)!.GetWidget<List>()!.GetChildSlot(1)!.GetWidget<Text>()!;
    private readonly AudioStream _stream;

    public string Name
    {
        get => NameText.Content;
        set => NameText.Content = value;
    }

    public TrackPlayerWidget(string name, AudioStream stream) : base(
        new Sizer()
        {
            HeightOverride = 70.0f
        },
        new BackgroundBlur(new List(
            new Text("NAME", 40)
            {
                Padding = new WidgetPadding()
                {
                    Top = 0.0f,
                    Bottom = 5.0f,
                }
            }, new Text("00:00 - 00:00", 30)
            {
                Padding = new WidgetPadding()
                {
                    Top = 5.0f,
                    Bottom = 5.0f,
                }
            })
        {
            Direction = List.Axis.Vertical,
            Padding = new WidgetPadding(5.0f,10.0f)
        })
        {
            Tint = Color.Black.Clone(a: 0.2f),
        })
    {
        NameText.Content = name;

        _stream = stream;
        Padding = 10.0f;
        FetchCover().ConfigureAwait(false);
    }

    public async Task FetchCover()
    {
        // var data = (await SAudioPlayer.Get().SpClient.Search.GetTracksAsync($"{NameText.Content} official track")).FirstOrDefault();
        // if (data == null)
        // {
        //     Console.WriteLine($"Failed to find art for {NameText.Content}");
        //     return;
        // }
        //
        //
        // var thumb = data.Album.Images.MaxBy(c => c.Height * c.Width)!.Url;
        //     
        // Console.WriteLine($"Using thumb {thumb} for {NameText.Content}");
        // GetChildSlot(0)?.GetWidget<Sizer>()?.AddChild(new Fitter(new AsyncWebImage(thumb))
        // {
        //     FittingMode = FitMode.Cover
        // });
    }

    protected string FormatTime(double secs)
    {
        return
            $"{((int)Math.Floor(secs / 60)).ToString().PadLeft(2, '0')}:{((int)(secs % 60)).ToString().PadLeft(2, '0')}";
    }

    public override void Collect(WidgetFrame frame, DrawInfo info)
    {
        StatusText.Content = $"{FormatTime(_stream.Position)} - {FormatTime(_stream.Length)}";
        base.Collect(frame, info);
    }
    
    protected override bool OnCursorDown(CursorDownEvent e) => _stream.IsPlaying ? _stream.Pause() : _stream.Play();

    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
        _stream.Dispose();
    }
}