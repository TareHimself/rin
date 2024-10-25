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

    public MainPanel()
    {
        var filePicker = new FilePicker();
        var filePickerSlot = AddChild(new PanelSlot(filePicker)
        {
            MaxAnchor = 0.5f,
            MinAnchor = 0.5f,
            Alignment = 0.5f,
            SizeToContent = true
        });

        filePicker.OnFileSelected += s =>
        {
            if (s.Length == 0) return;
            OnFileSelected(s);
        };

        AddChild(new PanelSlot(TrackPlayers)
        {
            SizeToContent = true,
            MinAnchor = 0.0f,
            MaxAnchor = new Vector2<float>(0.0f, 1.0f)
        });

        AddChild(new PanelSlot(new FpsWidget()
        {
            FontSize = 30
        })
        {
            SizeToContent = true,
            MinAnchor = new Vector2<float>(1.0f, 0.0f),
            MaxAnchor = new Vector2<float>(1.0f, 0.0f),
            Alignment = new Vector2<float>(1.0f, 0.0f)
        });
    }

    public override void OnSlotUpdated(Slot slot)
    {
        base.OnSlotUpdated(slot);
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