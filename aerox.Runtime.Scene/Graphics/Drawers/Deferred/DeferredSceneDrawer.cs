using aerox.Runtime.Graphics;
using aerox.Runtime.Graphics.Material;

namespace aerox.Runtime.Scene.Graphics.Drawers.Deferred;

public class DeferredSceneDrawer : SceneDrawer
{

    protected GBuffer? Images;
    protected DeviceBuffer? GlobalBuffer;
    protected MaterialInstance? _defaultMaterial;
    
    public override void Draw(Frame frame)
    {
        throw new NotImplementedException();
    }

    public DeferredSceneDrawer(Scene scene) : base(scene)
    {
        
    }


    public override void Start()
    {
        base.Start();
        GlobalBuffer = GraphicsModule.Get().GetAllocator()
            .NewUniformBuffer<SceneGlobalBuffer>(sequentialWrite: false, debugName: "Scene Global Buffer");

        _defaultMaterial = 
    }
}