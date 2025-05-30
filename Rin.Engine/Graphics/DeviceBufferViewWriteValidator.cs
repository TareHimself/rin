﻿using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics;

/// <summary>
///     A utility class used to ensure a device buffer has each section written only once
/// </summary>
public class DeviceBufferViewWriteValidator(IDeviceBufferView view) : IDeviceBufferView
{
    public IDeviceBuffer Buffer { get; } = new DeviceBufferWriteValidator(view.Buffer);
    public ulong Offset => view.Offset;
    public ulong Size => view.Size;

    public VkBuffer NativeBuffer => view.NativeBuffer;

    public IDeviceBufferView GetView(ulong offset, ulong size)
    {
        return view.GetView(offset, size);
    }
}