using rin.Core;
using rin.Core.Animation;
using rin.Core.Extensions;
using rin.Core.Math;
using rin.Graphics;
using rin.Widgets;
using rin.Widgets.Animation;
using rin.Widgets.Containers;
using rin.Widgets.Content;
using rin.Widgets.Graphics.Quads;
using rin.Windows;
using Rect = rin.Widgets.Containers.Rect;

namespace WidgetTest;

[RuntimeModule(typeof(SWidgetsModule))]
public class SWidgetTestModule : RuntimeModule
{
    public void TestWrapping(WindowRenderer renderer)
    {
        if (SWidgetsModule.Get().GetWindowSurface(renderer) is { } surf)
        {
            var list = new WrapList()
            {
                Axis = Axis.Row,
            };
            
            surf.Add(new Panel()
            {
                Slots = [
                new PanelSlot()
                {
                    Child = new ScrollList()
                    {
                        Slot = new ListSlot
                        {
                            Child = list,
                            Fit = CrossFit.Fill
                        },
                        Clip = Clip.None
                    },
                    MinAnchor = 0.0f,
                    MaxAnchor = 1.0f
                },
                new PanelSlot
                {
                    Child = new Rect
                    {
                        Child = new FpsWidget
                        {
                            FontSize = 30,
                        },
                        Padding = new Padding(20.0f),
                        BorderRadius = 10.0f,
                        BackgroundColor = Color.Black.Clone(a: 0.7f)
                    },
                    SizeToContent = true,
                    MinAnchor = new Vector2<float>(1.0f, 0.0f),
                    MaxAnchor = new Vector2<float>(1.0f, 0.0f),
                    Alignment = new Vector2<float>(1.0f, 0.0f)
                }]
            });

            var rand = new Random();
            surf.Window.OnKey += (e) =>
            {
                if (e is { State: KeyState.Pressed, Key: Key.Equal })
                {
                    Task.Run(() => Platform.SelectFile("Select Images", filter: "*.png;*.jpg;*.jpeg", multiple: true)).After(
                        (p) =>
                        {
                            foreach (var path in p)
                                list.AddChild(new Sizer()
                                {
                                    WidthOverride = rand.Next(300,300),
                                    HeightOverride = 300,
                                    Child = new AsyncFileImage(path)
                                    {
                                        BorderRadius = 30.0f
                                    },
                                    Padding = 10.0f,
                                });
                        });
                    
                }
                
                if (e is { State: KeyState.Pressed, Key: Key.Minus })
                {
                    list.AddChild(new Sizer()
                    {
                        WidthOverride = rand.Next(300, 300),
                        HeightOverride = 300,
                        Child = new PrettyWidget(),
                        Padding = 10.0f,
                    });
                }
            };
        }
    }

    public void TestSimple(WindowRenderer renderer)
    {
        var surf = SWidgetsModule.Get().GetWindowSurface(renderer);
        //SWidgetsModule.Get().GetOrCreateFont("Arial").ConfigureAwait(false);
        // surf?.Add(new TextInputBox("I expect this very long text to wrap if the space is too small"));
        surf?.Add(new TextBox("A"));
    }

    public void TestBlur(WindowRenderer renderer)
    {
        
        var surf = SWidgetsModule.Get().GetWindowSurface(renderer);

        if (surf == null) return;


        var switcher = new Switcher();

        var infoText = new TextBox("Background Blur Using Blit", 40);

        surf.Add(new Panel
        {
            Slots =
            [
                new PanelSlot
                {
                    Child = switcher,
                    MaxAnchor = 1.0f
                },
                new PanelSlot
                {
                    Child = new Canvas
                    {
                        Paint = (self, t, d) =>
                        {
                            d.AddRect(t.Transform, self.GetContentSize(), color: Color.Black.Clone(a: 0.6f));
                        }
                    },
                    MinAnchor = 0.0f,
                    MaxAnchor = 1.0f
                },
                new PanelSlot
                {
                    Child = infoText,
                    SizeToContent = true,
                    Alignment = 0f,
                    MinAnchor = 0f,
                    MaxAnchor = 0f
                },
                new PanelSlot
                {
                    Child = new TextInputBox("Example Text", 100),
                    SizeToContent = true,
                    Alignment = 0.5f,
                    MinAnchor = 0.5f,
                    MaxAnchor = 0.5f
                }
            ]
        });

        var frames = 0;
        SRuntime.Get().OnTick += d =>
        {
            frames++;
            infoText.Content = $"Frame {frames}"; //$"Focused ${panel.Surface?.FocusedWidget}";
        };

        surf.Window.OnKey += (e) =>
        {
            var switcherSlots = switcher.GetSlotsCount();
            if (switcherSlots > 0)
            {
                if (e is { State: KeyState.Pressed or KeyState.Repeat, Key: Key.Left })
                {
                    var newIndex = switcher.SelectedIndex - 1;
                    if (newIndex < 0)
                    {
                        newIndex = switcherSlots + newIndex;
                    }

                    switcher.SelectedIndex = newIndex;
                    return;
                }

                if (e is { State: KeyState.Pressed or KeyState.Repeat, Key: Key.Right })
                {
                    var newIndex = switcher.SelectedIndex + 1;
                    if (newIndex >= switcherSlots)
                    {
                        newIndex %= switcherSlots;
                    }

                    switcher.SelectedIndex = newIndex;
                    return;
                }
            }

            if (e is { State: KeyState.Pressed, Key: Key.F })
            {
                switcher.RotateTo(0, 360, 2).Then().RotateTo(360, 0, 2);
                return;
            }

            if (e is { State: KeyState.Pressed, Key: Key.Equal })
            {
                var p = Platform.SelectFile("Select Images", filter: "*.png;*.jpg;*.jpeg", multiple: true);
                foreach (var path in p)
                    switcher.AddChild(new Fitter
                    {
                        Child = new AsyncFileImage(path),
                        FittingMode = FitMode.Cover,
                        Clip = Clip.Bounds,
                        Padding = new Padding(100.0f)
                    });
            }
        };
    }


    public void TestClip(WindowRenderer renderer)
    {
        if (SWidgetsModule.Get().GetWindowSurface(renderer) is { } surface)
        {
            surface.Add(new Panel()
            {
                Slots =
                [
                    new PanelSlot
                    {
                        Child = new Sizer
                        {
                            Child = new Fitter
                            {
                                Child = new AsyncFileImage(""),
                                FittingMode = FitMode.Cover,
                                Clip = Clip.Bounds
                            },
                            WidthOverride = 600,
                            HeightOverride = 600
                        },
                        MinAnchor = 0.5f,
                        MaxAnchor = 0.5f,
                        Size = 200.0f,
                        Alignment = 0.5f
                    }
                ]
            });
        }
    }

    public void TestText(WindowRenderer renderer)
    {
        var surf = SWidgetsModule.Get().GetWindowSurface(renderer);

        if (surf == null) return;

        var panel = surf.Add(new Panel()
        {
            Slots =
            [
                new PanelSlot
                {
                    Child = new TextBox("Test Text", 40),
                    SizeToContent = true,
                    Alignment = 0f,
                    MinAnchor = 0f,
                    MaxAnchor = 0f
                }
            ]
        });
    }

    public override void Startup(SRuntime runtime)
    {
        base.Startup(runtime);
        Console.WriteLine("CREATING WINDOW");
        SGraphicsModule.Get().OnRendererCreated += TestWrapping;
        
        if (SWindowsModule.Get().CreateWindow(500, 500, "Rin Widget Test") is { } window)
        {
            window.OnCloseRequested += (_) =>
            {
                SRuntime.Get().RequestExit();
            };
            
            window.OnKey += (e =>
            {
                if (e is { Key: Key.Up, State: KeyState.Pressed })
                {
                    window.CreateChild(500, 500, "test child");
                }
                
                if (e is { Key: Key.Enter, State: KeyState.Pressed, IsAltDown: true })
                {
                    window.SetFullscreen(!window.IsFullscreen());
                }
                
            });
        }
        //TestText();
    }
}