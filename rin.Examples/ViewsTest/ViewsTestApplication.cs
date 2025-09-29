using System.Numerics;
using rin.Examples.Common.Views;
using Rin.Framework;
using Rin.Framework.Audio;
using Rin.Framework.Extensions;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Vulkan;
using Rin.Framework.Graphics.Windows;
using Rin.Framework.Video;
using Rin.Framework.Views;
using Rin.Framework.Views.Composite;
using Rin.Framework.Views.Content;
using Rin.Framework.Views.Graphics.Quads;
using Rin.Framework.Views.Layouts;

namespace rin.Examples.ViewsTest;

public class ViewsTestApplication : Application
{
    public static readonly int TileSize = 400;
    public override IGraphicsModule CreateGraphicsModule()
    {
        return new VulkanGraphicsModule();
    }

    public override IViewsModule CreateViewsModule()
    {
        return new ViewsModule();
    }

    public override IAudioModule CreateAudioModule()
    {
        return new BassAudioModule();
    }

    protected override void OnStartup()
    {
        {
            var manager = IViewsModule.Get().FontManager;
            if (manager.GetFont("Noto Sans") is { } font)
                manager
                    .PrepareAtlas(font, Enumerable.Range(32, 127).Select(c => (char)c).Where(c => c.IsPrintable()))
                    .Wait();
        }
        IGraphicsModule.Get().OnWindowRendererCreated += TestWrapping;
        IGraphicsModule.Get().OnWindowCreated += OnWindowCreated;
        IGraphicsModule.Get().CreateWindow("Views Test", new Extent2D(500), WindowFlags.Visible | WindowFlags.Resizable);
    }

    protected override void OnShutdown()
    {
        
    }
    
    private void OnWindowCreated(IWindow window)
    {
        window.OnClose += _ =>
        {
            if (window.Parent != null)
                window.Dispose();
            else
                RequestExit();
        };

        window.OnKey += e =>
        {
            if (e is { Key: InputKey.Up, State: InputState.Pressed })
                window.CreateChild("test child", new Extent2D(500));

            if (e is { Key: InputKey.Enter, State: InputState.Pressed, IsAltDown: true })
                window.SetFullscreen(!window.IsFullscreen);
        };
    }
    
    private void TestAnimation(IWindowRenderer renderer)
    {
        if (IViewsModule.Get().GetWindowSurface(renderer) is { } surf)
        {
            var list = new List
            {
                Axis = Axis.Row 
            };
            //https://samplelib.com/lib/preview/webm/sample-30s.webm
            var source = new FileVideoSource(Platform.SelectFile("Select a webm video", filter: "*.webm").First());//new HttpVideoSource(new Uri("https://samplelib.com/lib/preview/webm/sample-30s.webm"));// Platform.SelectFile("Select a webm video", filter: "*.webm").First();
            //var source = new HttpVideoSource(new Uri("https://b.catgirlsare.sexy/yTpGNCU13fu_.webm"));
            surf.Add(new Panel
            {
                Slots =
                [
                    new PanelSlot()
                    {
                        Child = new Fitter
                        {
                            Child = VideoPlayer.FromSource(source),
                            FittingMode = FitMode.Contain,
                            Padding = 50.0f,
                            Clip = Clip.Bounds
                        },
                        // Child = new Fitter
                        // {
                        //     Child = VideoPlayer.FromFile(Platform.SelectFile("Select a webm video",filter: "*.webm").First()),
                        //     FittingMode = FitMode.Contain,
                        //     Padding = new Padding(10.0f),
                        //     Clip = Clip.None,
                        // },
                        MinAnchor = Vector2.Zero,
                        MaxAnchor = Vector2.One,
                    },
                    new PanelSlot
                    {
                        Child = list,
                        MinAnchor = new Vector2(0.5f),
                        MaxAnchor = new Vector2(0.5f),
                        Alignment = new Vector2(0.5f),
                        SizeToContent = true
                    }
                     ,
                     new PanelSlot
                     {
                         Child = new BackgroundBlur
                         {
                             Child = new FpsView(),
                             Padding = new Padding(20.0f),
                             Tint = Color.Black with { A = 0.7f },
                             Strength = 5,
                             Radius = 20
                         },
                         SizeToContent = true,
                         MinAnchor = new Vector2(1.0f, 0.0f),
                         MaxAnchor = new Vector2(1.0f, 0.0f),
                         Alignment = new Vector2(1.0f, 0.0f)
                     },
                ]
            });

            surf.Window.OnDrop += e =>
            {
                Task.Run(() =>
                {
                    foreach (var objPath in e.Paths)
                        list.Add(new TestAnimationSizer
                        {
                            WidthOverride = 200,
                            HeightOverride = 800,
                            Child = new AsyncFileImage(objPath)
                            {
                                BorderRadius = new Vector4(30.0f)
                            },
                            Padding = 10.0f
                        });
                });
            };
            var rand = new Random();
            surf.Window.OnKey += e =>
            {
                if (e is { State: InputState.Pressed, Key: InputKey.Equal })
                    Task.Run(() => Platform.SelectFile("Select Images", filter: "*.png;*.jpg;*.jpeg", multiple: true))
                        .After(p =>
                        {
                            foreach (var path in p)
                                list.Add(new TestAnimationSizer
                                {
                                    WidthOverride = 200,
                                    HeightOverride = 800,
                                    Child = new AsyncFileImage(path)
                                    {
                                        BorderRadius = new Vector4(30.0f)
                                    },
                                    Padding = 10.0f
                                });
                        });

                if (e is { State: InputState.Pressed, Key: InputKey.Minus })
                    list.Add(new TestAnimationSizer
                    {
                        WidthOverride = 200,
                        HeightOverride = 800,
                        Child = new PrettyView
                        {
                            Pivot = new Vector2(0.5f)
                        },
                        Padding = 10.0f
                    });

                if (e is { State: InputState.Pressed, Key: InputKey.Zero })
                    list.Add(new Sizer
                    {
                        WidthOverride = 200,
                        HeightOverride = 900,
                        Child = new Canvas
                        {
                            Paint = (canvas, transform, cmds) =>
                            {
                                var rect = Quad.Rect(transform, canvas.GetContentSize());
                                rect.Mode = Quad.RenderMode.ColorWheel;
                                cmds.AddQuads(rect);
                            }
                        },
                        Padding = 10.0f
                    });
            };
        }
    }
    
         public void TestWrapping(IWindowRenderer renderer)
     {
         if (IViewsModule.Get().GetWindowSurface(renderer) is { } surf)
         {
             var list = new WrapList
             {
                 Axis = Axis.Row
             };

             surf.Add(new Panel
             {
                 Slots =
                 [
                     new PanelSlot
                     {
                         Child = new ScrollList
                         {
                             Slots =
                             [
                                 new ListSlot
                                 {
                                     Child = list,
                                     Fit = CrossFit.Available,
                                     Align = CrossAlign.Center
                                 }
                             ],
                             Clip = Clip.None
                         },
                         MinAnchor = new Vector2(0.0f),
                         MaxAnchor = new Vector2(1.0f)
                     },
                     new PanelSlot
                     {
                         Child = new Rect
                         {
                             Child = new FpsView(),
                             Padding = new Padding(20.0f),
                             BorderRadius = new Vector4(10.0f),
                             Color = Color.Black with { A = 0.7f }
                         },
                         SizeToContent = true,
                         MinAnchor = new Vector2(1.0f, 0.0f),
                         MaxAnchor = new Vector2(1.0f, 0.0f),
                         Alignment = new Vector2(1.0f, 0.0f)
                     }
                 ]
             });
             surf.Window.OnDrop += e =>
             {
                 Task.Run(() =>
                 {
                     foreach (var objPath in e.Paths)
                         IApplication.Get().MainDispatcher.Enqueue(() => list.Add(new ListSlot
                         {
                             Child = new WrapContainer(new AsyncFileImage(objPath)
                             {
                                 BorderRadius = new Vector4(30.0f)
                             }),
                             Align = CrossAlign.Center
                         }));
                 });
             };

             var rand = new Random();
             surf.Window.OnKey += e =>
             {
                 if (e is { State: InputState.Pressed, Key: InputKey.Equal })
                     Task.Run(() => Platform.SelectFile("Select Images", filter: "*.png;*.jpg;*.jpeg", multiple: true))
                         .After(p =>
                         {
                             IApplication.Get().MainDispatcher.Enqueue(() =>
                             {
                                 foreach (var path in p)
                                     list.Add(new WrapContainer(new AsyncFileImage(path)
                                     {
                                         BorderRadius = new Vector4(30.0f)
                                     }));
                             });
                         });

                 if (e is { State: InputState.Pressed, Key: InputKey.Minus })
                     list.Add(new WrapContainer(new PrettyView
                     {
                         //Pivot = 0.5f,
                     }));

                 if (e is { State: InputState.Pressed, Key: InputKey.Zero })
                     list.Add(new WrapContainer(new Canvas
                     {
                         Paint = (canvas, transform, cmds) =>
                         {
                             var size = canvas.GetContentSize();
                             // var rect = Quad.Rect(transform, canvas.GetContentSize());
                             // rect.Mode = Quad.RenderMode.ColorWheel;
                             // cmds.AddQuads(rect);
                             var a = Vector2.Zero;
                             cmds.AddQuadraticCurve(transform, new Vector2(0.0f), new Vector2(size.Y),
                                 new Vector2(0.0f, size.Y), color: Color.Red);
                             //cmds.AddCubicCurve(transform,new Vector2(0.0f),new Vector2(size.Y),new Vector2(0.0f,size.Y),new Vector2(size.Y,size.Y), color: Color.Red);
                         }
                     }));
             };
         }
     }
}