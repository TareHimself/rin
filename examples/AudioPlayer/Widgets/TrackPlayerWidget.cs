using System.Net;
using rin.Core;
using rin.Audio;
using rin.Graphics;
using rin.Core.Math;
using rin.Widgets;
using rin.Widgets.Animation;
using rin.Widgets.Containers;
using rin.Widgets.Content;
using rin.Widgets.Events;
using rin.Widgets.Graphics;
using MathNet.Numerics;
using SixLabors.ImageSharp.PixelFormats;
using TerraFX.Interop.Vulkan;
using Image = SixLabors.ImageSharp.Image;

namespace AudioPlayer.Widgets;


public class TrackPlayerWidget : WCOverlay
{
    private WText NameText => GetSlot(1)!.GetWidget<WCBlur>()!.GetSlot(0)!.GetWidget<WCList>()!.GetSlot(0)!.GetWidget<WText>()!;
    private WText StatusText => GetSlot(1)!.GetWidget<WCBlur>()!.GetSlot(0)!.GetWidget<WCList>()!.GetSlot(1)!.GetWidget<WText>()!;
    private readonly AudioStream _stream;
    private double lastTime = SRuntime.Get().GetElapsedRuntimeTimeSeconds();
    public string Name
    {
        get => NameText.Content;
        set => NameText.Content = value;
    }

    public TrackPlayerWidget(string name, AudioStream stream) : base()
    {
        AddChild(new WCSizer()
        {
            HeightOverride = 50.0f
        });
        AddChild(new WCBlur(new WCList([
            new WText("NAME", 40)
            {
                Padding = new WidgetPadding()
                {
                    Top = 0.0f,
                    Bottom = 5.0f,
                }
            },
            new WText("00:00 - 00:00", 30)
            {
                Padding = new WidgetPadding()
                {
                    Top = 5.0f,
                    Bottom = 5.0f,
                }
            }
        ])
        {
            Direction = WCList.Axis.Vertical,
            Padding = new WidgetPadding(5.0f, 10.0f)
        }){
            Tint = Color.Black.Clone(a: 0.2f),
        });
        NameText.Content = name;

        _stream = stream;
        Padding = 10.0f;
        FetchCover().ConfigureAwait(false);
    }

    public async Task FetchCover()
    {
        var data = (await SAudioPlayer.Get().SpClient.Search.GetTracksAsync($"{NameText.Content} official track")).FirstOrDefault();
        if (data == null)
        {
            Console.WriteLine($"Failed to find art for {NameText.Content}");
            return;
        }
        
        
        var thumb = data.Album.Images.MaxBy(c => c.Height * c.Width)!.Url;
            
        Console.WriteLine($"Using thumb {thumb} for {NameText.Content}");
        GetSlot(0)?.GetWidget<WCSizer>()?.AddChild(new AsyncWebCover(thumb)
        {
            BorderRadius = 20.0f
        });
    }

    protected string FormatTime(double secs)
    {
        return
            $"{((int)Math.Floor(secs / 60)).ToString().PadLeft(2, '0')}:{((int)(secs % 60)).ToString().PadLeft(2, '0')}";
    }
    
    public override void CollectContent(TransformInfo info, DrawCommands drawCommands)
    {
        StatusText.Content = $"{FormatTime(_stream.Position)} - {FormatTime(_stream.Length)}";
        base.CollectContent(info, drawCommands);
        
        var now = SRuntime.Get().GetElapsedRuntimeTimeSeconds();
        var diff = now - lastTime;
        lastTime = now;
        var offset = GetOffset();
        var result = MathUtils.InterpolateTo(offset.X, Hovered ? 40.0f : 0.0f,
            (float)diff, 150f);
        SetOffset(new Vector2<float>(result,offset.Y));
    }

    protected override bool OnCursorDown(CursorDownEvent e) => _stream.IsPlaying ? _stream.Pause() : _stream.Play();
    

    protected override void OnCursorLeave(CursorMoveEvent e)
    {
        base.OnCursorLeave(e);
    }
}