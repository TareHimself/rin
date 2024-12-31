using rin.Runtime.Core;

namespace rin.Editor;

internal class Program
{
    private static void Main(string[] args)
    {
        //Runtime.Instance.EnsureLoad("rin.Framework.Core","rin.Framework.Audio","rin.Framework.Graphics","rin.Windows","rin.Framework.Views");
        SRuntime.Get().Run();
    }

    // private class TestImage : Image
    // {
    //     public TestImage()
    //     {
    //         Tint = Color.Blue;
    //     }
    //
    //     protected override void OnCursorEnter(CursorMoveEvent e)
    //     {
    //         base.OnCursorEnter(e);
    //         Tint = Color.Green;
    //     }
    //
    //     protected override void OnCursorLeave(CursorMoveEvent e)
    //     {
    //         base.OnCursorLeave(e);
    //         Tint = Color.Blue;
    //     }
    //
    //     public override Vector2<float> ComputeDesiredSize()
    //     {
    //         return new Vector2<float>(200, 100);
    //     }
    // }
    //
    // private class AsyncFileImage : Image
    // {
    //     private float _alpha = 0.0f;
    //     private float _alphaTarget = 0.0f;
    //     
    //     
    //     public AsyncFileImage() : base()
    //     {
    //         
    //     }
    //     public AsyncFileImage(string filePath) : this()
    //     {
    //         Task.Run(() => LoadFile(filePath));
    //     }
    //
    //     public AsyncFileImage(string filePath, Action<AsyncFileImage> loadCallback) : this()
    //     {
    //         Task.Run(() => LoadFile(filePath).Then(() => loadCallback.Invoke(this)));
    //     }
    //
    //     private async Task LoadFile(string filePath)
    //     {
    //         using var imgData = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(filePath);
    //         var imgRawData = new byte[imgData.Width * imgData.Height * Marshal.SizeOf<Rgba32>()];
    //         imgData.CopyPixelDataTo(imgRawData);
    //         Texture = new Texture(imgRawData, new VkExtent3D
    //             {
    //                 width = (uint)imgData.Width,
    //                 height = (uint)imgData.Height,
    //                 depth = 1
    //             },
    //             Texture.Format.Rgba32,
    //             Texture.Filter.Linear,
    //             Texture.Tiling.Repeat);
    //     }
    //
    //     public override void Draw(WidgetFrame frame, DrawInfo info)
    //     {
    //         _alpha = MathUtils.InterpolateTo(_alpha, _alphaTarget,(float)Runtime.Instance.GetLastDeltaSeconds(), 0.8f);
    //         
    //         var sin = (float)Math.Sin(Runtime.Instance.GetTimeSinceCreation() * 1.3f);
    //         var borderRadius = float.Abs(sin) * 100.0f;
    //         BorderRadius = 100.0f * _alpha;
    //         base.Draw(frame, info);
    //     }
    //
    //     protected override void OnCursorEnter(CursorMoveEvent e)
    //     {
    //         base.OnCursorEnter(e);
    //         _alphaTarget = 1.0f;
    //     }
    //
    //     protected override void OnCursorLeave(CursorMoveEvent e)
    //     {
    //         base.OnCursorLeave(e);
    //         _alphaTarget = 0.0f;
    //     }
    // }
    //
    // private class Rect : View
    // {
    //
    //     public Rect() : base()
    //     {
    //         Pivot = 0.5f;
    //     }
    //     public override Vector2<float> ComputeDesiredSize() => new Vector2<float>(250, 250);
    //
    //     public override void Draw(WidgetFrame frame, DrawInfo info)
    //     {
    //         var sin = (float)Math.Sin(Runtime.Instance.GetTimeSinceCreation() * 1.3f);
    //
    //         Angle = sin * 180.0f;
    //         var sinAlpha = (sin + 1.0f) / 2.0f;
    //         var r = info.AccountFor(this);
    //         frame.AddRect(r.Transform,new (250,250),new (float.Abs(sin) * 50.0f));
    //     }
    // }
    //
    //
    // [RuntimeModule(typeof(WidgetsModule), typeof(GraphicsModule))]
    // private class GameModule : RuntimeModule
    // {
    //     private Channel? _audioStream;
    //     private Text? _fpsWidget;
    //
    //     public override async void Startup(Runtime runtime)
    //     {
    //         base.Startup(runtime);
    //         
    //         var widgetSubsystem = runtime.GetModule<WidgetsModule>()!;
    //         
    //         widgetSubsystem.AddFont(@"D:\Github\vengine\Roboto\Roboto-Regular.ttf");
    //         widgetSubsystem.AddFont(@"D:\Github\vengine\futura\FUTURA55REGULAR.TTF");
    //         //widgetSubsystem.GetOrCreateFont("Roboto").ConfigureAwait(false);
    //
    //         var window = runtime.GetModule<GraphicsModule>().GetMainWindow()!;
    //         window.OnKey += async e => { Console.WriteLine("Key Pressed {0}", e.State); };
    //
    //         var widgetRoot = widgetSubsystem.GetRoot(window);
    //         if (widgetRoot == null) return;
    //         
    //         var scrollBox = new ScrollableList(new TextBox(50));
    //         var panel = new Panel();
    //         var slot = panel.AddChild(scrollBox)!;
    //         slot.minAnchor = 0.0f;
    //         slot.maxAnchor = 1.0f;
    //         //slot.alignment = new Vector2<double>(0.5, 0.5);
    //         widgetRoot.Add(panel);
    //         // slot.alignment = new (0.5f, 0.5f);
    //         // slot.sizeToContent = true;
    //         widgetRoot.Add(panel);
    //         _audioStream = Stream.FromFile(@"D:\BH & Kirk Cosier - Slipping Away (ft. Cheney).wav");
    //         _audioStream.SetVolume(0.1f);
    //         //_audioStream.Play();
    //         Console.WriteLine("DONE");
    //         var numTicks = 0;
    //         runtime.OnTick += delta =>
    //         {
    //             numTicks++;
    //             if (_fpsWidget == null) return;
    //             _fpsWidget.Content = $"Ticks => {numTicks}";
    //             //_fpsWidget.Content = $"";
    //         };
    //         var mainWindow = Runtime.Instance.GetModule<GraphicsModule>().GetMainWindow()!;
    //         mainWindow.OnChar += e => { Console.WriteLine($"Char event {e.Data}"); };
    //
    //         mainWindow.OnKey += e =>
    //         {
    //             if (e is { State: InputKeyState.Pressed, Key: InputKey.Key3 })
    //                 Platform.SelectFile("Select Images", filter: "*.png;*.jpg;*.jpeg", multiple: true).Then(async p =>
    //                 {
    //                     foreach (var path in p)
    //                     {
    //                         var asyncFileImage = new AsyncFileImage(path, tex =>
    //                         {
    //                             var width = 600;
    //                             var height = tex.GetDesiredSize().Height / tex.GetDesiredSize().Width * width;
    //         
    //                             scrollBox.AddChild(new Sizer(tex)
    //                             {
    //                                 WidthOverride = width,
    //                                 HeightOverride = height
    //                             });
    //                         })
    //                         {
    //                             BorderRadius = new Vector4<float>(50.0f)
    //                         };
    //                     }
    //                 });
    //         };
    //     }
    // }
}