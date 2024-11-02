using rin.Audio;
using rin.Core.Math;
using rin.Widgets;
using rin.Widgets.Containers;
using rin.Widgets.Graphics;
using Clip = rin.Widgets.Clip;

namespace AudioPlayer.Widgets;

public class MainPanel : PanelContainer
{
    private readonly ScrollContainer _trackPlayers = new ScrollContainer()
    {
        Axis = Axis.Column,
        Clip = Clip.None
    };

    public MainPanel()
    {
        var filePicker = new FilePicker();
        Slots =
        [
            new PanelContainerSlot
            {
                Child = _trackPlayers,
                SizeToContent = true,
                MinAnchor = 0.0f,
                MaxAnchor = new Vector2<float>(0.0f, 1.0f)
            },
            new PanelContainerSlot
            {
                Child = new FpsWidget
                {
                    FontSize = 30,
                },
                SizeToContent = true,
                MinAnchor = new Vector2<float>(1.0f, 0.0f),
                MaxAnchor = new Vector2<float>(1.0f, 0.0f),
                Alignment = new Vector2<float>(1.0f, 0.0f)
            },
            new PanelContainerSlot
            {
                Child = filePicker,
                MaxAnchor = 0.5f,
                MinAnchor = 0.5f,
                Alignment = 0.5f,
                SizeToContent = true
            }
        ];

        filePicker.OnFileSelected += s =>
        {
            if (s.Length == 0) return;
            OnFileSelected(s);
        };
    }
    
    private void OnFileSelected(string[] files)
    {
        foreach (var file in files)
        {
            var player = new TrackPlayer(Path.GetFileNameWithoutExtension(file), AudioStream.FromFile(file));
            _trackPlayers.AddChild(player);
        }
    }
}