namespace Rin.Engine.Graphics.Descriptors;

public readonly struct BufferWrite(in DeviceBufferView view, BufferType type)
{
    public readonly DeviceBufferView Buffer = view;
    public readonly BufferType Type = type;
}