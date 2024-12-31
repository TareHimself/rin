using rin.Framework.Scene.Components;
using rin.Framework.World.Components;

namespace rin.Framework.World.Entities;

public class Entity
{
    public Scene.Scene Scene { get; }

    private readonly Dictionary<Type, List<Component>> _components = new();
    private readonly List<Component> _componentsList = [];
    private readonly List<SceneComponent> _renderedComponents = [];
    public SceneComponent? RootComponent;

    public Entity(Scene.Scene scene)
    {
        Scene = scene;
    }
}