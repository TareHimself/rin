using Rin.Core.Audio;
using Rin.Core.Audio.Effects;

namespace Examples.AudioEffects;

public interface IEffectSlot : IDisposable
{
    bool IsOn { get; }

    void Toggle();
}

/// <summary>
///     One effect that can be added to and removed from a group, keeping its parameters while it is off.
/// </summary>
public sealed class EffectSlot<TParams>(IAudioEffectDescriptor<TParams> descriptor, IAudioGroup group) : IEffectSlot
    where TParams : unmanaged
{
    private IEffectController<TParams>? _controller;
    private TParams _parameters = new();

    public bool IsOn => _controller is not null;

    public TParams Parameters
    {
        get => _parameters;
        set
        {
            _parameters = value;
            if (_controller is { } controller) controller.Parameters = value;
        }
    }

    public void Toggle()
    {
        if (_controller is not null)
        {
            Dispose();
            return;
        }

        _controller = group.AddEffect(descriptor);
        _controller.Parameters = _parameters;
    }

    public void Dispose()
    {
        _controller?.Dispose();
        _controller = null;
    }
}
