using aerox.Runtime;
using aerox.Runtime.Graphics;
using aerox.Runtime.Math;
using aerox.Runtime.Scene;
using aerox.Runtime.Widgets;
using aerox.Runtime.Widgets.Containers;
using aerox.Runtime.Widgets.Content;
using aerox.Runtime.Windows;
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
        
        var text = new WText();
        
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