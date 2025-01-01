using rin.Framework.Scene.Entities;

namespace rin.Framework.Scene.Components;

public class Component(Entity owner) : IComponent
{
    public string Id { get; set; } = string.Empty;
    public Entity Owner { get; set; } = owner;
}