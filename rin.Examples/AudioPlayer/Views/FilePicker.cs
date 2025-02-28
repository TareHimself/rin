using System.Numerics;
using Rin.Engine.Core;
using Rin.Engine.Core.Animation;
using Rin.Engine.Core.Extensions;
using Rin.Engine.Views;
using Rin.Engine.Views.Composite;
using Rin.Engine.Views.Content;
using Rin.Engine.Views.Events;
using Rin.Engine.Views.Graphics;

namespace rin.Examples.AudioPlayer.Views;

public class FilePicker : Button
{
    private float _animDuration = 0.1f;

    public event Action<string[]>? OnFileSelected;

    private readonly TextBox _statusText = new TextBox
    {
        Content = "Select File's",
        FontSize = 24.0f
    };
    
    public FilePicker() : base()
    {
        BackgroundColor = Color.Red;
        BorderRadius = new Vector4(10.0f);
        Padding = 20.0f;
        OnReleased += (_, __) =>
        {
            _statusText.Content = "Selecting...";
            Platform.SelectFileAsync("Select File's To Play", multiple: true, filter: "*.wav;*.ogg;*.flac;*.mp3")
                .After(FileSelected);
        };
        Child = _statusText;
    }

    protected void FileSelected(string[] files)
    {
        OnFileSelected?.Invoke(files);
        _statusText.Content = "Select File's";
    }

    protected override void OnCursorEnter(CursorMoveEvent e)
    {
        this
            .StopAll()
            .Transition(BackgroundColor, Color.Green, (c) => BackgroundColor = c,_animDuration)
            .Transition(BorderRadius,new Vector4(20.0f),(c) => BorderRadius = c,_animDuration);
        base.OnCursorEnter(e);
    }

    protected override void OnCursorLeave()
    {
        this
            .StopAll()
            .Transition(BackgroundColor, Color.Red, (c) => BackgroundColor = c,_animDuration)
            .Transition(BorderRadius,new Vector4(10.0f),(c) => BorderRadius = c,_animDuration);
        base.OnCursorLeave();
    }
    
}