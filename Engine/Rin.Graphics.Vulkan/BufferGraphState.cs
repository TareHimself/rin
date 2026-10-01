using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;

namespace Rin.Graphics.Vulkan;

/// <summary>
///     The usage and operation of the last render graph action on a buffer.
/// </summary>
public readonly record struct BufferGraphState(GraphBufferUsage Usage, ResourceOperation Operation);
