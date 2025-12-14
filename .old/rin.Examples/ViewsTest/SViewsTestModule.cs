// using System.Numerics;
// using Rin.Framework;
// using Rin.Framework.Animation;
// using Rin.Framework.Audio;
// using Rin.Framework.Extensions;
// using Rin.Framework.Graphics;
// using Rin.Framework.Graphics.Windows;
// using Rin.Framework.Math;
// using Rin.Framework.Views;
// using Rin.Framework.Views.Animation;
// using Rin.Framework.Views.Composite;
// using Rin.Framework.Views.Content;
// using Rin.Framework.Views.Events;
// using Rin.Framework.Views.Graphics;
// using Rin.Framework.Views.Graphics.Quads;
// using Rin.Framework.Views.Layouts;
// using rin.Examples.Common.Views;
// using rin.Examples.ViewsTest.Panels;
// using Rin.Framework.Video;
// using Rect = Rin.Framework.Views.Composite.Rect;
//
// namespace rin.Examples.ViewsTest;
//
// [Module(typeof(SViewsModule),typeof(SAudioModule))]
// [AlwaysLoad]
// public class SViewsTestModule : IModule
// {
//     
//
//     public void Start(IApplication app)
//     {
//         
//
//         //TestText();
//     }
//
//     public void Stop(IApplication app)
//     {
//     }
//
//     public void TestWrapping(IWindowRenderer renderer)
//     {
//         if (SViewsModule.Get().GetWindowSurface(renderer) is { } surf)
//         {
//             var list = new WrapList
//             {
//                 Axis = Axis.Row
//             };
//
//             surf.Add(new Panel
//             {
//                 Slots =
//                 [
//                     new PanelSlot
//                     {
//                         Child = new ScrollList
//                         {
//                             Slots =
//                             [
//                                 new ListSlot
//                                 {
//                                     Child = list,
//                                     Fit = CrossFit.Available,
//                                     Align = CrossAlign.Center
//                                 }
//                             ],
//                             Clip = Clip.None
//                         },
//                         MinAnchor = new Vector2(0.0f),
//                         MaxAnchor = new Vector2(1.0f)
//                     },
//                     new PanelSlot
//                     {
//                         Child = new Rect
//                         {
//                             Child = new FpsView(),
//                             Padding = new Padding(20.0f),
//                             BorderRadius = new Vector4(10.0f),
//                             Color = Color.Black with { A = 0.7f }
//                         },
//                         SizeToContent = true,
//                         MinAnchor = new Vector2(1.0f, 0.0f),
//                         MaxAnchor = new Vector2(1.0f, 0.0f),
//                         Alignment = new Vector2(1.0f, 0.0f)
//                     }
//                 ]
//             });
//             surf.Window.OnDrop += e =>
//             {
//                 Task.Run(() =>
//                 {
//                     foreach (var objPath in e.Paths)
//                         IApplication.Get().MainDispatcher.Enqueue(() => list.Add(new ListSlot
//                         {
//                             Child = new WrapContainer(new AsyncFileImage(objPath)
//                             {
//                                 BorderRadius = new Vector4(30.0f)
//                             }),
//                             Align = CrossAlign.Center
//                         }));
//                 });
//             };
//
//             var rand = new Random();
//             surf.Window.OnKey += e =>
//             {
//                 if (e is { State: InputState.Pressed, Key: InputKey.Equal })
//                     Task.Run(() => Platform.SelectFile("Select Images", filter: "*.png;*.jpg;*.jpeg", multiple: true))
//                         .After(p =>
//                         {
//                             IApplication.Get().MainDispatcher.Enqueue(() =>
//                             {
//                                 foreach (var path in p)
//                                     list.Add(new WrapContainer(new AsyncFileImage(path)
//                                     {
//                                         BorderRadius = new Vector4(30.0f)
//                                     }));
//                             });
//                         });
//
//                 if (e is { State: InputState.Pressed, Key: InputKey.Minus })
//                     list.Add(new WrapContainer(new PrettyView
//                     {
//                         //Pivot = 0.5f,
//                     }));
//
//                 if (e is { State: InputState.Pressed, Key: InputKey.Zero })
//                     list.Add(new WrapContainer(new Canvas
//                     {
//                         Paint = (canvas, transform, cmds) =>
//                         {
//                             var size = canvas.GetContentSize();
//                             // var rect = Quad.Rect(transform, canvas.GetContentSize());
//                             // rect.Mode = Quad.RenderMode.ColorWheel;
//                             // cmds.AddQuads(rect);
//                             var a = Vector2.Zero;
//                             cmds.AddQuadraticCurve(transform, new Vector2(0.0f), new Vector2(size.Y),
//                                 new Vector2(0.0f, size.Y), color: Color.Red);
//                             //cmds.AddCubicCurve(transform,new Vector2(0.0f),new Vector2(size.Y),new Vector2(0.0f,size.Y),new Vector2(size.Y,size.Y), color: Color.Red);
//                         }
//                     }));
//             };
//         }
//     }
//
//     
//
//     public void TestSimple(IWindowRenderer renderer)
//     {
//         var surf = SViewsModule.Get().GetWindowSurface(renderer);
//         //SViewsModule.Get().GetOrCreateFont("Arial").ConfigureAwait(false);
//         surf?.Add(new TextInputBox
//         {
//             Content = "AVAVAV",
//             FontSize = 240
//         });
//         //surf?.Add(new TextBox("A"));
//     }
//
//     public void TestBlur(IWindowRenderer renderer)
//     {
//         var surf = SViewsModule.Get().GetWindowSurface(renderer);
//
//         if (surf == null) return;
//         var panel = new HoverToReveal();
//
//         surf.Add(panel);
//
//         surf.Window.OnKey += e =>
//         {
//             if (e is { State: InputState.Pressed, Key: InputKey.Equal })
//             {
//                 var p = Platform.SelectFile("Select Images", filter: "*.png;*.jpg;*.jpeg", multiple: true);
//                 foreach (var path in p)
//                     panel.AddImage(new AsyncFileImage(path));
//             }
//         };
//     }
//
//
//     public void TestClip(IWindowRenderer renderer)
//     {
//         if (SViewsModule.Get().GetWindowSurface(renderer) is { } surface)
//             surface.Add(new Sizer
//             {
//                 Padding = 20.0f,
//                 Child = new Rect
//                 {
//                     Child = new Fitter
//                     {
//                         Child = new AsyncFileImage(
//                             @"C:\Users\Taree\Downloads\Wallpapers-20241117T001814Z-001\Wallpapers\wallpaperflare.com_wallpaper (49).jpg"),
//                         FittingMode = FitMode.Cover,
//                         Clip = Clip.Bounds
//                     },
//                     Color = Color.Red
//                 }
//             });
//     }
//
//     public void TestText(IWindowRenderer renderer)
//     {
//         var surf = SViewsModule.Get().GetWindowSurface(renderer);
//
//         if (surf == null) return;
//
//         var panel = surf.Add(new Panel
//         {
//             Slots =
//             [
//                 new PanelSlot
//                 {
//                     Child = new TextBox
//                     {
//                         Content = "Test Text",
//                         FontSize = 40
//                     },
//                     SizeToContent = true,
//                     Alignment = Vector2.Zero,
//                     MinAnchor = Vector2.Zero,
//                     MaxAnchor = Vector2.Zero
//                 }
//             ]
//         });
//     }
//
//     public void TestCanvas(IWindowRenderer renderer)
//     {
//         if (SViewsModule.Get().GetWindowSurface(renderer) is { } surf)
//             surf.Add(new Canvas
//             {
//                 Paint = (self, transform, cmds) =>
//                 {
//                     var topLeft = Vector2.Zero.Transform(transform);
//                     var angle = (float.Sin(SFramework.Get().GetTimeSeconds()) + 1.0f) / 2.0f * 180.0f;
//                     cmds.AddRect(Matrix4x4.Identity.Translate(self.GetContentSize() / 2.0f).Rotate2dDegrees(angle),
//                         self.GetContentSize() - new Vector2(100.0f), Color.White);
//                 }
//             });
//     }
//
//     
// }