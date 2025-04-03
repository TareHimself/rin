using System.Numerics;
using Rin.Engine;
using Rin.Engine.Animation;
using Rin.Engine.Extensions;
using Rin.Engine.Views;
using Rin.Engine.Views.Composite;
using Rin.Engine.Views.Content;
using Rin.Engine.Views.Events;

namespace rin.Examples.AudioPlayer.Views;

public class FilePicker : Button
{
    private readonly float _animDuration = 0.1f;

    private readonly TextBox _statusText = new()
    {
        Content = "Select File's",
        FontSize = 24.0f
    };

    public FilePicker()
    {
        Color = Color.Red;
        BorderRadius = new Vector4(10.0f);
        Padding = 20.0f;
        OnReleased += (_, __) =>
        {
            _statusText.Content = "Selecting...";
            Platform.SelectFileAsync("Select File's To Play", true, "*.wav;*.ogg;*.flac;*.mp3")
                .After(FileSelected);
        };
        Child = _statusText;
    }

    public event Action<string[]>? OnFileSelected;

    protected void FileSelected(string[] files)
    {
        OnFileSelected?.Invoke(files);
        _statusText.Content = "Select File's";
    }

    protected override void OnCursorEnter(CursorMoveSurfaceEvent e)
    {
        this
            .StopAll()
            .Transition(Color, Color.Green, c => Color = c, _animDuration)
            .Transition(BorderRadius, new Vector4(20.0f), c => BorderRadius = c, _animDuration);
        base.OnCursorEnter(e);
    }

    protected override void OnCursorLeave()
    {
        this
            .StopAll()
            .Transition(Color, Color.Red, c => Color = c, _animDuration)
            .Transition(BorderRadius, new Vector4(10.0f), c => BorderRadius = c, _animDuration);
        base.OnCursorLeave();
    }
}