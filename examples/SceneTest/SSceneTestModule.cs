using rin.Core;
using rin.Graphics;
using rin.Core.Math;
using rin.Scene;
using rin.Runtime.Widgets;
using rin.Runtime.Views.Containers;
using rin.Runtime.Views.Content;

using SceneTest.entities;

namespace SceneTest;

[RuntimeModule(typeof(SSceneModule),typeof(SWidgetsModule))]
public class SSceneTestModule : RuntimeModule
{
    public override void Startup(SRuntime runtime)
    {
        base.Startup(runtime);

        var window = SWindowsModule.Get().CreateWindow(500, 500, "Aerox Scene Test");
        var scene = SSceneModule.Get().AddScene<TestScene>();

        var surf = SWidgetsModule.Get().GetWindowSurface();
        
        var text = new TextBox();
        
        var viewport = surf?.Add(new Viewport(scene,text));
        
        var panel = surf?.Add(new Panel());
        
        var textSlot = panel?.AddChild(text);
        
        textSlot?.Mutate((c) =>
        {
            c.SizeToContent = true;
            c.MinAnchor = 0.0f;
            c.MaxAnchor = 0.0f;
        });
        
        var entity = scene.AddEntity<CameraEntity>();
        var meshEntity = scene.AddEntity<MeshEntity>();
        scene.SetViewTarget(entity);

        window.OnKey += (k) =>
        {
            if (k.Key is Key.Left or Key.Right)
            {
                if (k.Key is Key.Left)
                {
                    
                }
                else
                {
                    
                }
            }
        };
    }
}