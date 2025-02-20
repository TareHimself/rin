#pragma once
#include "DescriptorSet.h"
#include "IDeviceImage.h"
#include "ImageFilter.h"
#include "ImageTiling.h"
#include "BoundTexture.h"
#include "rin/core/Disposable.h"
#include "rin/core/memory.h"
#include "rin/io/Task.h"

namespace rin::rhi
{
    class ITextureManager : public Disposable
    {
    public:
        virtual DescriptorSet * GetDescriptorSet() = 0;

        virtual io::Task<int> CreateTexture(void * data,const vk::Extent3D& size, ImageFormat format,
            ImageFilter filter = ImageFilter::Linear,
            ImageTiling tiling = ImageTiling::Repeat, bool mips = false, const std::string& debugName = "Texture") = 0;

        virtual void FreeTextures(const std::vector<int>& textureIds) = 0;

        virtual Shared<IDeviceImage> GetTextureImage(int textureId) = 0;

        virtual bool IsTextureIdValid(int textureId) = 0;
    };
}
