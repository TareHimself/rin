#pragma once
#include "DescriptorAllocator.h"
#include "ITextureManager.h"
#include "rin/core/HandleGenerator.h"
#ifndef RIN_MAX_TEXTURES
#define RIN_MAX_TEXTURES 2048
#endif
namespace rin::rhi
{
    class DefaultTextureManager : public ITextureManager
    {
        std::mutex _texturesMutex{};
        std::unordered_set<int> _freeIds{};
        std::vector<Shared<BoundTexture>> _textures{};
        DescriptorSet * _set;
        DescriptorAllocator _allocator{
            RIN_MAX_TEXTURES,
            std::vector{
                PoolSizeRatio{
                    vk::DescriptorType::eCombinedImageSampler,
                    1.0f
                }
            },
            vk::DescriptorPoolCreateFlagBits::eUpdateAfterBind
        };
        Shared<IDeviceImage> _defaultTexture{};
    protected:
        void OnDispose() override;
        Shared<BoundTexture> GetTexture(int textureId) const;
        void UpdateTextures(const std::vector<int>& textureIds);
    public:
        DefaultTextureManager();
        DescriptorSet * GetDescriptorSet() override;
        io::Task<int> CreateTexture(void* data, const vk::Extent3D& size, ImageFormat format, ImageFilter filter,
            ImageTiling tiling, bool mips, const std::string& debugName) override;
        void FreeTextures(const std::vector<int>& textureIds) override;
        
        Shared<IDeviceImage> GetTextureImage(int textureId) override;
        bool IsTextureIdValid(int textureId) override;
    };
}
