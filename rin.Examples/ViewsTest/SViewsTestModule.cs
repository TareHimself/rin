using System.Numerics;
using Rin.Framework;
using Rin.Framework.Animation;
using Rin.Framework.Audio;
using Rin.Framework.Extensions;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Windows;
using Rin.Framework.Math;
using Rin.Framework.Views;
using Rin.Framework.Views.Animation;
using Rin.Framework.Views.Composite;
using Rin.Framework.Views.Content;
using Rin.Framework.Views.Events;
using Rin.Framework.Views.Graphics;
using Rin.Framework.Views.Graphics.Quads;
using Rin.Framework.Views.Layouts;
using rin.Examples.Common.Views;
using rin.Examples.ViewsTest.Panels;
using Rin.Framework.Video;
using Rect = Rin.Framework.Views.Composite.Rect;

namespace rin.Examples.ViewsTest;

[Module(typeof(SViewsModule),typeof(SAudioModule))]
[AlwaysLoad]
public class SViewsTestModule : IModule
{
    private static readonly int _tileSize = 400;

    public void Start(SApplication application)
    {
        {
            var manager = SViewsModule.Get().GetFontManager();
            if (manager.GetFont("Noto Sans") is { } font)
                manager
                    .PrepareAtlas(font, Enumerable.Range(32, 127).Select(c => (char)c).Where(c => c.IsPrintable()))
                    .Wait();
        }
        SGraphicsModule.Get().OnRendererCreated += TestAnimation;
        SGraphicsModule.Get().OnWindowCreated += OnWindowCreated;
        SGraphicsModule.Get().CreateWindow("Views Test", new Extent2D(500), WindowFlags.Visible | WindowFlags.Resizable);


        //TestText();
    }

    public void Stop(SApplication application)
    {
    }

    public void TestWrapping(IWindowRenderer renderer)
    {
        if (SViewsModule.Get().GetWindowSurface(renderer) is { } surf)
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
                        SApplication.Get().DispatchMain(() => list.Add(new ListSlot
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
                            SApplication.Get().DispatchMain(() =>
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

    private void TestAnimation(IWindowRenderer renderer)
    {
        if (SViewsModule.Get().GetWindowSurface(renderer) is { } surf)
        {
            var list = new List
            {
                Axis = Axis.Row 
            };
            //https://samplelib.com/lib/preview/webm/sample-30s.webm
            var source = new FileVideoSource(Platform.SelectFile("Select a webm video", filter: "*.webm").First());//new HttpVideoSource(new Uri("https://samplelib.com/lib/preview/webm/sample-30s.webm"));// Platform.SelectFile("Select a webm video", filter: "*.webm").First();
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

    public void TestSimple(IWindowRenderer renderer)
    {
        var surf = SViewsModule.Get().GetWindowSurface(renderer);
        //SViewsModule.Get().GetOrCreateFont("Arial").ConfigureAwait(false);
        surf?.Add(new TextInputBox
        {
            Content = "AVAVAV",
            FontSize = 240
        });
        //surf?.Add(new TextBox("A"));
    }

    public void TestBlur(IWindowRenderer renderer)
    {
        var surf = SViewsModule.Get().GetWindowSurface(renderer);

        if (surf == null) return;
        var panel = new HoverToReveal();

        surf.Add(panel);

        surf.Window.OnKey += e =>
        {
            if (e is { State: InputState.Pressed, Key: InputKey.Equal })
            {
                var p = Platform.SelectFile("Select Images", filter: "*.png;*.jpg;*.jpeg", multiple: true);
                foreach (var path in p)
                    panel.AddImage(new AsyncFileImage(path));
            }
        };
    }


    public void TestClip(IWindowRenderer renderer)
    {
        if (SViewsModule.Get().GetWindowSurface(renderer) is { } surface)
            surface.Add(new Sizer
            {
                Padding = 20.0f,
                Child = new Rect
                {
                    Child = new Fitter
                    {
                        Child = new AsyncFileImage(
                            @"C:\Users\Taree\Downloads\Wallpapers-20241117T001814Z-001\Wallpapers\wallpaperflare.com_wallpaper (49).jpg"),
                        FittingMode = FitMode.Cover,
                        Clip = Clip.Bounds
                    },
                    Color = Color.Red
                }
            });
    }

    public void TestText(IWindowRenderer renderer)
    {
        var surf = SViewsModule.Get().GetWindowSurface(renderer);

        if (surf == null) return;

        var panel = surf.Add(new Panel
        {
            Slots =
            [
                new PanelSlot
                {
                    Child = new TextBox
                    {
                        Content = "Test Text",
                        FontSize = 40
                    },
                    SizeToContent = true,
                    Alignment = Vector2.Zero,
                    MinAnchor = Vector2.Zero,
                    MaxAnchor = Vector2.Zero
                }
            ]
        });
    }

    public void TestCanvas(IWindowRenderer renderer)
    {
        if (SViewsModule.Get().GetWindowSurface(renderer) is { } surf)
            surf.Add(new Canvas
            {
                Paint = (self, transform, cmds) =>
                {
                    var topLeft = Vector2.Zero.Transform(transform);
                    var angle = (float.Sin(SApplication.Get().GetTimeSeconds()) + 1.0f) / 2.0f * 180.0f;
                    cmds.AddRect(Matrix4x4.Identity.Translate(self.GetContentSize() / 2.0f).Rotate2dDegrees(angle),
                        self.GetContentSize() - new Vector2(100.0f), Color.White);
                }
            });
    }

    private void OnWindowCreated(IWindow window)
    {
        window.OnClose += _ =>
        {
            if (window.Parent != null)
                window.Dispose();
            else
                SApplication.Get().RequestExit();
        };

        window.OnKey += e =>
        {
            if (e is { Key: InputKey.Up, State: InputState.Pressed })
                window.CreateChild("test child", new Extent2D(500));

            if (e is { Key: InputKey.Enter, State: InputState.Pressed, IsAltDown: true })
                window.SetFullscreen(!window.IsFullscreen);
        };
    }

    private class WrapContainer : Button
    {
        private readonly View _content;

        public WrapContainer(View content)
        {
            Color = Color.Transparent;
            _content = content;
            var sizer = new Sizer
            {
                WidthOverride = _tileSize,
                HeightOverride = _tileSize,
                Child = content,
                Padding = 10.0f
            };
            Child = sizer;
            content.Pivot = new Vector2(0.5f);
            OnReleased += (@event, button) =>
            {
                //_content.StopAll().RotateTo(360,0.5f).After().Do(() => _content.Angle = 0.0f);
                var transitionDuration = 0.8f;
                var method = EasingFunctions.EaseInOutCubic;
                sizer
                    .WidthTo(_tileSize * 4f + 60.0f, transitionDuration, easingFunction: method)
                    .HeightTo(_tileSize * 2f + 10.0f, transitionDuration, easingFunction: method)
                    .Delay(4)
                    .WidthTo(_tileSize, transitionDuration, easingFunction: method)
                    .HeightTo(_tileSize, transitionDuration, easingFunction: method);
            };
        }

        protected override Vector2 LayoutContent(in Vector2 availableSpace)
        {
            var size = base.LayoutContent(availableSpace);
            _content.Translate = _content.Size * .5f;
            return size;
        }

        protected override void CollectSelf(Matrix4x4 transform, CommandList cmds)
        {
            base.CollectSelf(transform, cmds);
        }

        public override void Collect(in Matrix4x4 transform, in Rin.Framework.Graphics.Rect2D clip, CommandList commands)
        {
            base.Collect(transform, clip, commands);
        }
    }

    private class TestAnimationSizer : Sizer
    {
        private float _width;

        public TestAnimationSizer()
        {
            _width = WidthOverride.GetValueOrDefault(0);
        }

        protected override void OnAddedToSurface(ISurface surface)
        {
            base.OnAddedToSurface(surface);
            _width = WidthOverride.GetValueOrDefault(0);
        }

        protected override void OnCursorEnter(CursorMoveSurfaceEvent e)
        {
            this.StopAll().WidthTo(HeightOverride.GetValueOrDefault(0), easingFunction: EasingFunctions.EaseInOutCubic);
        }

        protected override void OnCursorLeave()
        {
            this.StopAll().WidthTo(_width, easingFunction: EasingFunctions.EaseInOutCubic);
        }
    }
}