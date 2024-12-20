﻿using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using rin.Framework.Core;

namespace rin.Framework.Graphics;

/// <summary>
///     GPU Memory
/// </summary>
public abstract partial class DeviceMemory(Allocator allocator, IntPtr allocation) : Reservable
{
    public readonly IntPtr Allocation = allocation;
    protected readonly Allocator Allocator = allocator;
}