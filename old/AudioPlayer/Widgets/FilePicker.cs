using aerox.Runtime;
using aerox.Runtime.Extensions;
using aerox.Runtime.Widgets;
using aerox.Runtime.Widgets.Animation;
using aerox.Runtime.Widgets.Defaults.Containers;
using aerox.Runtime.Widgets.Defaults.Content;
using aerox.Runtime.Widgets.Graphics.Commands;
using aerox.Runtime.Widgets.Events;

namespace AudioPlayer.Widgets;

public class FilePicker : Button
{
    private bool _hasInit = false;

    public event Action<string[]>? OnFileSelected;

    protected Color BgColor = Color.Red;
    protected readonly Text StatusText = new Text("Select File's");
    
    public FilePicker() : base()
    {
        
    }

    protected override void OnAddedToSurface(WidgetSurface widgetSurface)
    {
        base.OnAddedToSurface(widgetSurface);
        if (_hasInit) return;
        _hasInit = true;
        StatusText.Padding = 20.0f;
        AddChild(StatusText);
        this.RunAction(new SimpleAnimationAction((s, time) =>
        {
            // var num = (float)Math.Sin(time);
            // s.Target.Angle = 60.0f * num;
            return true;
        }));
    }
    
    protected override void CollectSelf(WidgetFrame frame, DrawInfo myInfo)
    {
        base.CollectSelf(frame, myInfo);
        frame.AddCommands(new SimpleRect(myInfo.Transform, GetDrawSize())
        {
            Color = BgColor,
            BorderRadius = 20.0f,
        });
    }

    protected void FileSelected(string[] files)
    {
        OnFileSelected?.Invoke(files);
        StatusText.Content = "Select File's";
    }

    protected override bool OnCursorDown(CursorDownEvent e)
    {
        StatusText.Content = "Selecting...";
        Platform.SelectFileAsync("Select File's To Play",multiple:true,filter:"*.wav;*.ogg;*.flac;*.mp3").Then(FileSelected).ConfigureAwait(false);
        return base.OnCursorDown(e);
    }

    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
        StatusText.Dispose();
    }

    protected override void OnCursorEnter(CursorMoveEvent e)
    {
        BgColor = Color.Green;
        base.OnCursorEnter(e);
    }

    protected override void OnCursorLeave(CursorMoveEvent e)
    {
        BgColor = Color.Red;
        base.OnCursorLeave(e);
        
    }
}