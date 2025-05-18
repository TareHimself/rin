#pragma once
enum AttachmentFormat {
    R8,
    R16,
    R32,
    RG8,
    RG16,
    RG32,
    RGBA8,
    RGBA16,
    RGBA32,
}

[__AttributeUsage(_AttributeTargets.Var),__AttributeUsage(_AttributeTargets.Function)]
struct AttachmentAttribute
{
    AttachmentFormat format;
};

[__AttributeUsage(_AttributeTargets.Function)]
struct BlendNoneAttribute
{
};

[__AttributeUsage(_AttributeTargets.Function)]
struct BlendUIAttribute
{
};

[__AttributeUsage(_AttributeTargets.Function)]
struct BlendOpaqueAttribute
{
};

[__AttributeUsage(_AttributeTargets.Function)]
struct BlendTranslucentAttribute
{
};

[__AttributeUsage(_AttributeTargets.Function)]
struct DepthAttribute
{
};

[__AttributeUsage(_AttributeTargets.Function)]
struct StencilAttribute
{
};