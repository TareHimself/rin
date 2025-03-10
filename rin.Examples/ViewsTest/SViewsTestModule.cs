using System.Numerics;
using Rin.Assets;
using rin.Examples.Common.Views;
using rin.Examples.ViewsTest.Panels;
using Rin.Engine.Core;
using Rin.Engine.Core.Animation;
using Rin.Engine.Core.Curves;
using Rin.Engine.Core.Extensions;
using Rin.Engine.Core.Math;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Windows;
using Rin.Engine.Views;
using Rin.Engine.Views.Animation;
using Rin.Engine.Views.Composite;
using Rin.Engine.Views.Content;
using Rin.Engine.Views.Events;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Graphics.Quads;
using Rin.Engine.Views.Layouts;
using Rect = Rin.Engine.Views.Composite.Rect;
using Utils = Rin.Engine.Graphics.Utils;

namespace rin.Examples.ViewsTest;

[Module(typeof(SViewsModule)),AlwaysLoad]
public class SViewsTestModule : IModule
{

    private static int _tileSize = 400;
    class WrapContainer : Button
    {
        private readonly View _content;
        public WrapContainer(View content)
        {
            BackgroundColor = Color.Transparent;
            _content = content;
            var sizer = new Sizer()
            {
                WidthOverride = _tileSize,
                HeightOverride = _tileSize,
                Child = content,
                Padding = 10.0f,
            };
            Child = sizer;
            content.Pivot = new Vector2(0.5f);
            OnReleased += (@event, button) =>
            {
               //_content.StopAll().RotateTo(360,0.5f).After().Do(() => _content.Angle = 0.0f);
               var transitionDuration = 0.8f;
               var method = EasingFunctions.EaseInOutCubic;
               sizer
                   .WidthTo(_tileSize * 4f + 60.0f,duration: transitionDuration,easingFunction: method)
                   .HeightTo(_tileSize * 2f + 10.0f,duration: transitionDuration,easingFunction: method)
                   .Delay(4)
                   .WidthTo(_tileSize,duration: transitionDuration,easingFunction: method)
                   .HeightTo(_tileSize,duration: transitionDuration,easingFunction: method);
            };
        }

        protected override Vector2 LayoutContent(Vector2 availableSpace)
        {
            var size = base.LayoutContent(availableSpace);
            _content.Translate = _content.Size * .5f;
            return size;
        }
    }
    public void TestWrapping(IWindowRenderer renderer)
    {
        if (SViewsModule.Get().GetWindowSurface(renderer) is { } surf)
        {
            
            var scheduler = new Scheduler();

            SEngine.Get().OnUpdate += (_) => scheduler.Update();
            
            var list = new WrapList()
            {
                Axis = Axis.Row,
            };

            surf.Add(new Panel()
            {
                
                Slots =
                [
                    new PanelSlot()
                    {
                        Child = new ScrollList()
                        {
                            Slots = [
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
                        MaxAnchor = new Vector2(1.0f),
                    },
                    new PanelSlot
                    {
                        // Child = new Rect                                    
                        // {
                        //     Child = new FpsView
                        //     {
                        //         FontSize = 30
                        //     },
                        //     Padding = new Padding(20.0f),
                        //     BorderRadius = 10.0f,
                        //     BackgroundColor = Color.Black.Clone(a: 0.7f)
                        // },
                        Child = new BackgroundBlur()                                
                        {
                            Child = new FpsView(),
                            Padding = new Padding(20.0f),
                            Strength = 20.0f
                        },
                        SizeToContent = true,
                        MinAnchor = new Vector2(1.0f, 0.0f),
                        MaxAnchor = new Vector2(1.0f, 0.0f),
                        Alignment = new Vector2(1.0f, 0.0f)
                    }
                ]
            });
            surf.Window.OnDrop += (e) =>
            {
                Task.Run(() =>
                {
                    foreach (var objPath in e.Paths)
                    {
                        scheduler.Schedule(() => list.Add(new ListSlot
                        {
                            Child = new WrapContainer(new AsyncFileImage(objPath)
                            {
                                BorderRadius = new Vector4(30.0f)
                            }),
                            Align = CrossAlign.Center,
                        }));
                    }
                });
            };
            
            var rand = new Random();
            surf.Window.OnKey += (e) =>
            {
                if (e is { State: InputState.Pressed, Key: InputKey.Equal })
                {
                    Task.Run(() => Platform.SelectFile("Select Images", filter: "*.png;*.jpg;*.jpeg", multiple: true))
                        .After(
                            (p) =>
                            {
                                scheduler.Schedule(() =>
                                {
                                    foreach (var path in p)
                                        list.Add(new WrapContainer(new AsyncFileImage(path)
                                        {
                                            BorderRadius = new Vector4(30.0f)
                                        }));
                                });
                            });
                }

                if (e is { State: InputState.Pressed, Key: InputKey.Minus })
                {
                    list.Add(new WrapContainer(new PrettyView()
                    {
                        //Pivot = 0.5f,
                    }));
                }

                if (e is { State: InputState.Pressed, Key: InputKey.Zero })
                {
                    list.Add(new WrapContainer(new Canvas
                    {
                        Paint = ((canvas, transform, cmds) =>
                        {
                            var size = canvas.GetContentSize();
                            // var rect = Quad.Rect(transform, canvas.GetContentSize());
                            // rect.Mode = Quad.RenderMode.ColorWheel;
                            // cmds.AddQuads(rect);
                            var a = Vector2.Zero;
                            cmds.AddQuadraticCurve(transform,new Vector2(0.0f),new Vector2(size.Y),new Vector2(0.0f,size.Y), color: Color.Red);
                            //cmds.AddCubicCurve(transform,new Vector2(0.0f),new Vector2(size.Y),new Vector2(0.0f,size.Y),new Vector2(size.Y,size.Y), color: Color.Red);
                        })
                    }));
                }
            };
        }
    }


    class TestAnimationSizer : Sizer
    {
        private float _width;

        protected override void OnAddedToSurface(Surface surface)
        {
            base.OnAddedToSurface(surface);
            _width = WidthOverride.GetValueOrDefault(0);
        }

        public TestAnimationSizer()
        {
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

    private void TestAnimation(IWindowRenderer renderer)
    {
        if (SViewsModule.Get().GetWindowSurface(renderer) is { } surf)
        {
            var list = new List()
            {
                Axis = Axis.Row,
            };

            surf.Add(new Panel()
            {
                Slots =
                [
                    new PanelSlot()
                    {
                        Child = list,
                        MinAnchor = new Vector2(0.5f),
                        MaxAnchor = new Vector2(0.5f),
                        Alignment = new Vector2(0.5f),
                        SizeToContent = true
                    },
                    new PanelSlot
                    {
                        Child = new Rect
                        {
                            Child = new FpsView
                            {
                                FontSize = 30,
                                Content = "YOOOO"
                            },
                            Padding = new Padding(20.0f),
                            BorderRadius = new Vector4(10.0f),
                            BackgroundColor = Color.Black.Clone(a: 0.7f)
                        },
                        SizeToContent = true,
                        MinAnchor = new Vector2(1.0f, 0.0f),
                        MaxAnchor = new Vector2(1.0f, 0.0f),
                        Alignment = new Vector2(1.0f, 0.0f)
                    }
                ]
            });

            surf.Window.OnDrop += (e) =>
            {
                Task.Run(() =>
                {
                    foreach (var objPath in e.Paths)
                    {
                        list.Add(new TestAnimationSizer()
                        {
                            WidthOverride = 200,
                            HeightOverride = 800,
                            Child = new AsyncFileImage(objPath)
                            {
                                BorderRadius = new Vector4(30.0f)
                            },
                            Padding = 10.0f,
                        });
                    }
                });
            };
            var rand = new Random();
            surf.Window.OnKey += (e) =>
            {
                if (e is { State: InputState.Pressed, Key: InputKey.Equal })
                {
                    Task.Run(() => Platform.SelectFile("Select Images", filter: "*.png;*.jpg;*.jpeg", multiple: true))
                        .After(
                            (p) =>
                            {
                                foreach (var path in p)
                                    list.Add(new TestAnimationSizer()
                                    {
                                        WidthOverride = 200,
                                        HeightOverride = 800,
                                        Child = new AsyncFileImage(path)
                                        {
                                            BorderRadius = new Vector4(30.0f)
                                        },
                                        Padding = 10.0f,
                                    });
                            });
                }

                if (e is { State: InputState.Pressed, Key: InputKey.Minus })
                {
                    list.Add(new TestAnimationSizer()
                    {
                        WidthOverride = 200,
                        HeightOverride = 800,
                        Child = new PrettyView()
                        {
                            Pivot = new Vector2(0.5f),
                        },
                        Padding = 10.0f,
                    });
                }

                if (e is { State: InputState.Pressed, Key: InputKey.Zero })
                {
                    list.Add(new Sizer()
                    {
                        WidthOverride = 200,
                        HeightOverride = 900,
                        Child = new Canvas
                        {
                            Paint = ((canvas, transform, cmds) =>
                            {
                                var rect = Quad.Rect(transform, canvas.GetContentSize());
                                rect.Mode = Quad.RenderMode.ColorWheel;
                                cmds.AddQuads(rect);
                            })
                        },
                        Padding = 10.0f,
                    });
                }
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

        surf.Window.OnKey += (e) =>
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
        {
            surface.Add(new Sizer()
            {
                Padding = 20.0f,
                Child = new Rect()
                {
                    Child = new Fitter
                    {
                        Child = new AsyncFileImage(
                            @"C:\Users\Taree\Downloads\Wallpapers-20241117T001814Z-001\Wallpapers\wallpaperflare.com_wallpaper (49).jpg"),
                        FittingMode = FitMode.Cover,
                        Clip = Clip.Bounds
                    },
                    BackgroundColor = Color.Red
                }
            });
        }
    }

    public void TestText(IWindowRenderer renderer)
    {
        var surf = SViewsModule.Get().GetWindowSurface(renderer);

        if (surf == null) return;

        var panel = surf.Add(new Panel()
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

    private void OnWindowCreated(IWindow window)
    {
        window.OnCloseRequested += (_) =>
        {
            if (window.Parent != null)
            {
                window.Dispose();
            }
            else
            {
                SEngine.Get().RequestExit();
            }
        };

        window.OnKey += (e =>
        {
            if (e is { Key: InputKey.Up, State: InputState.Pressed })
            {
                window.CreateChild(500, 500, "test child",new CreateOptions()
                {
                    Visible = true,
                    Decorated = true,
                    Transparent = true,
                    Focused = false
                });
            }

            if (e is { Key: InputKey.Enter, State: InputState.Pressed, IsAltDown: true })
            {
                window.SetFullscreen(!window.IsFullscreen);
            }
        });
    }

    public void Start(SEngine engine)
    {
        var resources = RinAssets.Assets.GetAllResources().ToArray();
        SViewsModule.Get().GetFontManager().LoadSystemFonts();
        
        Common.Utils.RunMultithreaded((delta) =>
        {
            SGraphicsModule.Get().PollWindows();
            SViewsModule.Get().Update(delta);
            SGraphicsModule.Get().Collect();
        }, () =>
        {
            SGraphicsModule.Get().Execute();
        });
        
        // Common.Utils.RunMultithreaded((delta) =>
        // {
        //     SGraphicsModule.Get().PollWindows();
        //     SViewsModule.Get().Update(delta);
        //     SGraphicsModule.Get().Collect();
        // }, () =>
        // {
        //     SGraphicsModule.Get().Execute();
        // });

        // {
        //     var fontManager = SViewsModule.Get().GetFontManager();
        //     if (fontManager.TryGetFont("Arial", out var family))
        //     {
        //         fontManager.Prepare(family, Enumerable.Range(33, 126 - 33).Select(c => (char)c).ToArray());
        //     }
        // }R
        SGraphicsModule.Get().OnRendererCreated += TestWrapping;
        SGraphicsModule.Get().OnWindowCreated += OnWindowCreated;
        SGraphicsModule.Get().CreateWindow(500, 500, "Rin View Test", new CreateOptions()
        {
            Visible = true,
            Decorated = true,
            Transparent = false,
            Focused = true
        });
        //TestText();
    }

    public void Stop(SEngine engine)
    {
        
    }
}