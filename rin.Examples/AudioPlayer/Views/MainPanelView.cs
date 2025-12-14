using System.Numerics;
using Rin.Framework.Views;
using Rin.Framework.Views.Composite;
using Rin.Framework.Views.Layouts;
using rin.Examples.Common.Views;
using Rin.Framework.Audio;
using Clip = Rin.Framework.Views.Clip;

namespace rin.Examples.AudioPlayer.Views;

public class MainPanelView : PanelView
{
    private readonly ScrollListView _trackPlayers = new()
    {
        Axis = Axis.Column,
        Clip = Clip.None
    };

    public MainPanelView()
    {
        var filePicker = new FilePicker();
        Slots =
        [
            new PanelSlot
            {
                Child = _trackPlayers,
                //SizeToContent = true,
                MinAnchor = new Vector2(0.0f),
                MaxAnchor = new Vector2(1.0f) //new Vector2<float>(0.5f, 0.5f)
            },
            new PanelSlot
            {
                Child = new RectView
                {
                    Child = new FpsView
                    {
                        FontSize = 30
                    },
                    Padding = new Padding(20.0f),
                    BorderRadius = new Vector4(10.0f),
                    Color = Color.Black with { A = 0.7f }
                },
                SizeToContent = true,
                MinAnchor = new Vector2(1.0f, 0.0f),
                MaxAnchor = new Vector2(1.0f, 0.0f),
                Alignment = new Vector2(1.0f, 0.0f)
            },
            new PanelSlot
            {
                Child = filePicker,
                MaxAnchor = new Vector2(1f),
                MinAnchor = new Vector2(1f),
                Alignment = new Vector2(1f),
                Offset = new Vector2(-10.0f),
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
            var player = new TrackPlayer(Path.GetFileNameWithoutExtension(file),IAudioModule.Get().CreateStream(file));
            _trackPlayers.Add(player);
        }
    }
}