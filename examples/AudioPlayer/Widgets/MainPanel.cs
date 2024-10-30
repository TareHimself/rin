using rin.Audio;
using rin.Core.Math;
using rin.Widgets;
using rin.Widgets.Containers;
using rin.Widgets.Graphics;

namespace AudioPlayer.Widgets;

public class MainPanel : WCPanel
{
    private readonly WCScrollList _trackPlayers = new WCScrollList()
    {
        Direction = WCList.Axis.Vertical
    };

    public MainPanel()
    {
        AddChild(new PanelSlot(_trackPlayers)
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
        
        var filePicker = new FilePicker();
        AddChild(new PanelSlot(filePicker)
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
    }

    public override void OnSlotUpdated(Slot slot)
    {
        base.OnSlotUpdated(slot);
    }

    private void OnFileSelected(string[] files)
    {
        foreach (var file in files)
        {
            var player = new TrackPlayerWidget(Path.GetFileNameWithoutExtension(file), AudioStream.FromFile(file));
            _trackPlayers.AddChild(player);
        }
    }

    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
    }

    protected override void OnAddedToSurface(Surface surface)
    {
        base.OnAddedToSurface(surface);
    }

    public override void SetSize(Size2d size)
    {
        base.SetSize(size);
    }
}