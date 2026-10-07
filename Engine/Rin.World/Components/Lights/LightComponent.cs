using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Views;

namespace Rin.World.Components.Lights;

public abstract class LightComponent : WorldComponent
{
    private bool _propertiesDirty;

    [PublicAPI]
    public float Radiance
    {
        get;
        set => Set(ref field, value);
    } = 5.0f;

    [PublicAPI]
    public float Radius
    {
        get;
        set => Set(ref field, value);
    } = 10000.0f;

    [PublicAPI]
    public Color Color
    {
        get;
        set => Set(ref field, value);
    } = Color.White;

    protected bool ConsumePropertiesDirty()
    {
        var dirty = _propertiesDirty;
        _propertiesDirty = false;
        return dirty;
    }

    private void Set<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        _propertiesDirty = true;
    }
}
