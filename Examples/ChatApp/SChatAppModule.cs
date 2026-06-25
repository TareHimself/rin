// using ChatApp.Views;
// using Rin.Core;
// using Rin.Core.Graphics;
// using Rin.Core.Graphics.Windows;
// using Rin.Core.Views;
//
// namespace ChatApp;
//
// [Module(typeof(SViewsModule))]
// [AlwaysLoad]
// public class SChatAppModule : IModule, ISingletonGetter<SChatAppModule>
// {
//     private IWindow? _window;
//
//     public void Start(IApplication app)
//     {
//         _window = SGraphicsModule.Get().CreateWindow("Chat", new Extent2D(500));
//         _window.OnClose += _ => { _window.Dispose(); };
//         _window.GetViewSurface()?.Add(new MainView());
//     }
//
//     public void Stop(IApplication app)
//     {
//     }
//
//     public static SChatAppModule Get()
//     {
//         return SFramework.Get().GetModule<SChatAppModule>();
//     }
// }

