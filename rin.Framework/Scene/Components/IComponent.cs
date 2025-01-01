using rin.Framework.Scene.Entities;

namespace rin.Framework.Scene.Components;

public interface IComponent
{
    public string Id { get; set; }
    public Entity Owner { get; set; }
}