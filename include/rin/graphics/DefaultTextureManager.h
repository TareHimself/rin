#pragma once
#include "ITextureManager.h"

namespace rin::graphics
{
    class DefaultTextureManager : public ITextureManager
    {
    protected:
        void OnDispose() override;

    public:
        Weak<DescriptorSet> GetDescriptorSet() override;
        io::Task<int> CreateTexture(void* data, const vk::Extent3D& size, ImageFormat format, ImageFilter filter,
            ImageTiling tiling, bool mips, const std::string& debugName) override;
        void FreeTextures(const std::vector<int>& textureIds) override;
        Shared<ITexture> GetTexture(int textureId) override;
        Shared<IDeviceImage> GetTextureImage(int textureId) override;
        bool IsTextureIdValid(int textureId) override;
    };
}
