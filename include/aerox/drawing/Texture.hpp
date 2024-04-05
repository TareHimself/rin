#pragma once
#include "GpuNative.hpp"
#include "aerox/Object.hpp"
#include "aerox/assets/AssetMeta.hpp"
#include "aerox/assets/Image.hpp"
#include "aerox/assets/LiveAsset.hpp"
#include "gen/drawing/Texture.gen.hpp"

namespace aerox::drawing {
class DrawingSubsystem;

META_TYPE()

class Texture : public TOwnedBy<DrawingSubsystem>, public assets::LiveAsset, public GpuNative {

  std::shared_ptr<AllocatedImage> _gpuData;

  vk::SamplerAddressMode _tiling = vk::SamplerAddressMode::eRepeat;
  
  vk::Sampler _sampler = nullptr;
  
  META_PROPERTY()
  vk::Filter _filter = vk::Filter::eLinear;

  META_PROPERTY()
  vk::Format _format = vk::Format::eUndefined;
  
  META_PROPERTY()
  vk::Extent3D _size;

  META_PROPERTY()
  bool _mipMapped = true;
  
  std::vector<unsigned char> _data;
  
  void MakeSampler();

public:

  META_BODY()
  
  vk::Extent3D GetSize() const;
  
  std::weak_ptr<AllocatedImage> GetGpuData() const;
  vk::Sampler GetSampler() const;
  void SetMipMapped(bool newMipMapped);

  bool IsMipMapped() const;

  void SetGpuData(const std::shared_ptr<AllocatedImage> &allocation);
  virtual void SetTiling(vk::SamplerAddressMode tiling);
  virtual void SetFilter(vk::Filter filter);

  void ReadFrom(Buffer &store) override;

  void WriteTo(Buffer &store) override;

  void Upload() override;

  bool IsUploaded() const override;

  void OnDestroy() override;

  void OnInit(DrawingSubsystem * subsystem) override;

  bool Save(const fs::path& path);

  static uint32_t GetFormatChannels(const vk::Format& format);
  static std::shared_ptr<Texture> FromMemory(
      const std::vector<unsigned char> &data,
      vk::Extent3D size, vk::Format format,
      vk::Filter filter,
      vk::SamplerAddressMode tiling =
          vk::SamplerAddressMode::eRepeat);


  static std::shared_ptr<Texture> FromAllocated(
      const std::shared_ptr<AllocatedImage>& image,
      vk::Filter filter,
      vk::SamplerAddressMode tiling = vk::SamplerAddressMode::eRepeat);

  std::weak_ptr<AllocatedImage> GetGpuData();
  
  META_FUNCTION()
  static std::shared_ptr<Texture> Construct() { return newObject<Texture>(); }
};
}
