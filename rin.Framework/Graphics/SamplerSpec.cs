﻿using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics;

public struct SamplerSpec
{
    public ImageTiling Tiling;
    public ImageFilter Filter;

    public static implicit operator VkSampler(SamplerSpec spec)
    {
        return SGraphicsModule.Get().GetSampler(spec);
    }
}