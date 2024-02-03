#pragma once
#include "GpuNative.hpp"
#include "vengine/Object.hpp"
#include "vengine/assets/Asset.hpp"
#include "generated/vengine/drawing/Texture.reflect.hpp"
namespace vengine::drawing {
class Drawer;

RCLASS()
class Texture : public Object<Drawer>, public assets::Asset, public GpuNative {

  Ref<AllocatedImage> _gpuData;
  vk::Filter _filter = vk::Filter::eLinear;
  vk::SamplerAddressMode _tiling = vk::SamplerAddressMode::eRepeat;
  vk::Sampler _sampler = nullptr;
  vk::Format _format = vk::Format::eUndefined;
  vk::Extent3D _size;
  Array<unsigned char> _data;
  bool _mipMapped = true;

  void MakeSampler();
public:

  vk::Extent3D GetSize() const;
  WeakRef<AllocatedImage> GetGpuData() const;
  vk::Sampler GetSampler() const;
  void SetMipMapped(bool newMipMapped);

  bool IsMipMapped() const;

  void SetGpuData(const Ref<AllocatedImage> &allocation);
  virtual void SetTiling(vk::SamplerAddressMode tiling);
  virtual void SetFilter(vk::Filter filter);
  String GetSerializeId() override;

  void ReadFrom(Buffer &store) override;

  void WriteTo(Buffer &store) override;

  void Upload() override;
  
  bool IsUploaded() const override;

  void HandleDestroy() override;

  void Init(Drawer * outer) override;
  
  static Ref<Texture> FromData(Drawer * drawer, const Array<unsigned char> &data,vk::Extent3D size,vk::Format format,vk::Filter filter,vk::SamplerAddressMode tiling = vk::SamplerAddressMode::eRepeat);
};

REFLECT_IMPLEMENT(Texture)
}
