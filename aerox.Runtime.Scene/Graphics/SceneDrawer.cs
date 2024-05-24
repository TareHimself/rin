using aerox.Runtime.Graphics;
using aerox.Runtime.Graphics.Material;
using aerox.Runtime.Math;
using aerox.Runtime.Scene.Entities;
using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Scene.Graphics;

public abstract class SceneDrawer : Disposable, IDrawable, ILifeCycle
{
    public event Action<SceneFrame,Matrix4> OnDraw;
    public Scene OwningScene;
    
    public SceneDrawer(Scene scene)
    {
        OwningScene = scene;
    }
    
    public virtual void Start()
    {
        
    }

    protected override void OnDispose(bool isManual)
    {
        
    }

    public abstract void Draw(Frame frame);

    public abstract VkFormat[] GetAttachmentFormats();

    public virtual MaterialBuilder SceneMaterialBuilder()
    {
        return new MaterialBuilder();
    } 
    
    public void Tick(double deltaSeconds)
    {
        
    }
}