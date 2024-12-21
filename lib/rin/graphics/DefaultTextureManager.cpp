#include "rin/graphics/DefaultTextureManager.h"

namespace rin::graphics
{
    void DefaultTextureManager::OnDispose()
    {
        
    }

    Weak<DescriptorSet> DefaultTextureManager::GetDescriptorSet()
    {
        NOT_IMPLEMENTED
    }

    io::Task<int> DefaultTextureManager::CreateTexture(void* data, const vk::Extent3D& size, ImageFormat format,
        ImageFilter filter, ImageTiling tiling, bool mips, const std::string& debugName)
    {
        NOT_IMPLEMENTED
    }

    void DefaultTextureManager::FreeTextures(const std::vector<int>& textureIds)
    {
        NOT_IMPLEMENTED
    }

    Shared<ITexture> DefaultTextureManager::GetTexture(int textureId)
    {
        NOT_IMPLEMENTED
    }

    Shared<IDeviceImage> DefaultTextureManager::GetTextureImage(int textureId)
    {
        NOT_IMPLEMENTED
    }

    bool DefaultTextureManager::IsTextureIdValid(int textureId)
    {
        NOT_IMPLEMENTED
    }
}
