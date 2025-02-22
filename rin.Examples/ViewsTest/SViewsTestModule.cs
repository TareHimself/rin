using System.Numerics;
using rin.Examples.Common.Views;
using rin.Examples.ViewsTest.Panels;
using rin.Framework.Core;
using rin.Framework.Core.Animation;
using rin.Framework.Core.Extensions;
using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Graphics.Windows;
using rin.Framework.Views;
using rin.Framework.Views.Animation;
using rin.Framework.Views.Composite;
using rin.Framework.Views.Content;
using rin.Framework.Views.Events;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Graphics.Quads;
using rin.Framework.Views.Layouts;
using Rect = rin.Framework.Views.Composite.Rect;
using Utils = rin.Framework.Graphics.Utils;

namespace rin.Examples.ViewsTest;

[Module(typeof(SViewsModule)),AlwaysLoad]
public class SViewsTestModule : IModule
{

    class WrapContainer : Button
    {
        private readonly View _content;
        public WrapContainer(View content)
        {
            BackgroundColor = Color.Transparent;
            _content = content;
            var sizer = new Sizer()
            {
                WidthOverride = 300,
                HeightOverride = 300,
                Child = content,
                Padding = 10.0f,
            };
            Child = sizer;
            content.Pivot = new Vector2(0.5f);
            OnReleased += (@event, button) =>
            {
               //_content.StopAll().RotateTo(360,0.5f).After().Do(() => _content.Angle = 0.0f);
               sizer.WidthTo(620.0f).Delay(2).WidthTo(300.0f);
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

            SRuntime.Get().OnUpdate += (_) => scheduler.Update();
            
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
                                    Fit = CrossFit.Fill
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
                            Child = new FpsView
                            {
                                FontSize = 30
                            },
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
                        list.Add(new WrapContainer(new AsyncFileImage(objPath)
                        {
                            BorderRadius = new Vector4(30.0f)
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

        protected override void OnCursorEnter(CursorMoveEvent e)
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
                    Alignment = new Vector2(0f),
                    MinAnchor = new Vector2(0f),
                    MaxAnchor = new Vector2(0f)
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
                SRuntime.Get().RequestExit();
            }
        };

        window.OnKey += (e =>
        {
            if (e is { Key: InputKey.Up, State: InputState.Pressed })
            {
                window.CreateChild(500, 500, "test child");
            }

            if (e is { Key: InputKey.Enter, State: InputState.Pressed, IsAltDown: true })
            {
                window.SetFullscreen(!window.IsFullscreen);
            }
        });
    }

    public void Startup(SRuntime runtime)
    {
        Common.Utils.RunSingleThreaded((delta) =>
        {
            SGraphicsModule.Get().PollWindows();
            SViewsModule.Get().Update(delta);
            SGraphicsModule.Get().Collect();
        }, () =>
        {
            SGraphicsModule.Get().Execute();
        });

        SGraphicsModule.Get().OnRendererCreated += TestWrapping;
        SGraphicsModule.Get().OnWindowCreated += OnWindowCreated;
        SGraphicsModule.Get().CreateWindow(500, 500, "Rin View Test", new CreateOptions()
        {
            Visible = true,
            Decorated = true,
            Transparent = false,
            Focused = false
        });
        //TestText();
    }

    public void Shutdown(SRuntime runtime)
    {

    }
}