#pragma once

[__AttributeUsage(_AttributeTargets.Var)]
struct UpdateAfterBindAttribute
{
};

[__AttributeUsage(_AttributeTargets.Var)]
struct PartialAttribute
{
};

[__AttributeUsage(_AttributeTargets.Var)]
struct VariableAttribute
{
    int count;
};

[__AttributeUsage(_AttributeTargets.Var)]
struct AllStagesAttribute
{
};

[__AttributeUsage(_AttributeTargets.Var)]
struct PushAttribute
{
};

[__AttributeUsage(_AttributeTargets.Var)]
struct SampledTextureBindingAttribute
{
};

[__AttributeUsage(_AttributeTargets.Var)]
struct TextureBindingAttribute
{
};

[__AttributeUsage(_AttributeTargets.Var)]
struct TextureArrayBindingAttribute
{
};

[__AttributeUsage(_AttributeTargets.Var)]
struct CubemapBindingAttribute
{
};

[__AttributeUsage(_AttributeTargets.Var)]
struct StorageImageBindingAttribute
{
};

[__AttributeUsage(_AttributeTargets.Var)]
struct SamplerBindingAttribute
{
};

[__AttributeUsage(_AttributeTargets.Var)]
struct UniformBufferBindingAttribute
{
};

[__AttributeUsage(_AttributeTargets.Var)]
struct StorageBufferBindingAttribute
{
};


