namespace Rin.Shade;

// Marker types for Vulkan/Slang resources, used on a [ShaderBinding] field - Rin.Shade owns these
// since descriptor sets and bindings are Slang/Vulkan concepts, not Rin ones.
public struct Texture2D;

public struct Texture2DArray;

public struct TextureCube;

public struct SamplerState;
