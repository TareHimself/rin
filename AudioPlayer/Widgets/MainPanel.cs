using aerox.Runtime.Audio;
using aerox.Runtime.Math;
using aerox.Runtime.Widgets;
using aerox.Runtime.Widgets.Containers;

namespace AudioPlayer.Widgets;

public class MainPanel : Panel
{
    private bool _init = false;

    protected readonly ScrollableList TrackPlayers = new ScrollableList()
    {
        Direction = List.Axis.Vertical
    };

    protected override void OnAddedToSurface(WidgetSurface widgetSurface)
    {
        base.OnAddedToSurface(widgetSurface);

        if (_init) return;
        _init = true;

        var filePicker = new FilePicker();
        var filePickerSlot = AddChild(filePicker);
        if (filePickerSlot != null)
        {
            filePickerSlot.Mutate((d) =>
            {
                d.MaxAnchor = 0.5f;
                d.MinAnchor = 0.5f;
                d.Alignment = 0.5f;
                d.SizeToContent = true;
            });
        }

        filePicker.OnFileSelected += s =>
        {
            if (s.Length == 0) return;
            OnFileSelected(s);
        };

        AddChild(TrackPlayers)?.Mutate((s) =>
        {
            s.SizeToContent = true;
            s.MinAnchor = 0.0f;
            s.MaxAnchor = new Vector2<float>(0.0f, 1.0f);
        });

        AddChild(new FpsWidget()
        {
            FontSize = 30
        })?.Mutate((s) =>
        {
            s.SizeToContent = true;
            s.MinAnchor = new Vector2<float>(1.0f, 0.0f);
            s.MaxAnchor = new Vector2<float>(1.0f, 0.0f);
            s.Alignment = new Vector2<float>(1.0f, 0.0f);
        });
    }
    
    

    protected void OnFileSelected(string[] files)
    {
        foreach (var file in files)
        {
            var player = new TrackPlayerWidget(Path.GetFileNameWithoutExtension(file), AudioStream.FromFile(file));
            TrackPlayers.AddChild(player);
        }
    }

    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
    }
}