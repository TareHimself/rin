﻿using rin.Framework.Core;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics.Shaders;

public class CompiledShader
{
    public readonly Dictionary<uint, VkDescriptorSetLayout> DescriptorLayouts = [];
    public readonly List<Pair<VkShaderEXT, VkShaderStageFlags>> Shaders = [];
    public VkPipelineLayout PipelineLayout;
}