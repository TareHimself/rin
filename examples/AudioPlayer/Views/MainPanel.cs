using rin.Framework.Audio;
using rin.Framework.Core.Math;
using rin.Framework.Views;
using rin.Framework.Views.Composite;
using rin.Framework.Views.Layouts;
using Clip = rin.Framework.Views.Clip;
using Rect = rin.Framework.Views.Composite.Rect;

namespace AudioPlayer.Views;

public class MainPanel : Panel
{
    private readonly ScrollList _trackPlayers = new ScrollList()
    {
        Axis = Axis.Column,
        Clip = Clip.None
    };

    public MainPanel()
    {
        var filePicker = new FilePicker();
        Slots =
        [
            new PanelSlot
            {
                Child = _trackPlayers,
                //SizeToContent = true,
                MinAnchor = 0.0f,
                MaxAnchor = 1.0f//new Vector2<float>(0.5f, 0.5f)
            },
            new PanelSlot
            {
                Child = new Rect
                {
                    Child = new FpsView
                    {
                        FontSize = 30,
                    },
                    Padding = new Padding(20.0f),
                    BorderRadius = 10.0f,
                    BackgroundColor = Color.Black.Clone(a: 0.7f)
                },
                SizeToContent = true,
                MinAnchor = new Vec2<float>(1.0f, 0.0f),
                MaxAnchor = new Vec2<float>(1.0f, 0.0f),
                Alignment = new Vec2<float>(1.0f, 0.0f)
            },
            new PanelSlot
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
            var player = new TrackPlayer(Path.GetFileNameWithoutExtension(file), StreamChannel.FromFile(file));
            _trackPlayers.Add(player);
        }
    }
}