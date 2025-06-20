// using TerraFX.Interop.Vulkan;
//
// namespace Rin.Engine.Graphics;
//
// public interface DeviceBufferView
// {
//    
//
//     public ulong GetAddress()
//     {
//         return Buffer.GetAddress() + Offset;
//     }
//
//     public DeviceBufferView GetView(ulong offset, ulong size);
//
//     public unsafe void Write(void* src, ulong size, ulong offset = 0)
//     {
//         Buffer.Write(src, size, Offset + offset);
//     }
// }