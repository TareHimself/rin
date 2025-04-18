﻿namespace Rin.Engine.Graphics;

/// <summary>
///     GPU Memory
/// </summary>
public abstract class DeviceMemory(Allocator allocator, IntPtr allocation) : Reservable
{
    public readonly IntPtr Allocation = allocation;
    protected readonly Allocator Allocator = allocator;
}