#pragma once
#include "GpuNative.hpp"
#include "vengine/Object.hpp"
#include "vengine/assets/Asset.hpp"
#include "generated/drawing/Texture2D.reflect.hpp"

namespace vengine::drawing {
class DrawingSubsystem;

RCLASS()

class Texture2D : public Object<DrawingSubsystem>, public assets::Asset, public GpuNative {

  Managed<AllocatedImage> _gpuData;
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
  Ref<AllocatedImage> GetGpuData() const;
  vk::Sampler GetSampler() const;
  void SetMipMapped(bool newMipMapped);

  bool IsMipMapped() const;

  void SetGpuData(const Managed<AllocatedImage> &allocation);
  virtual void SetTiling(vk::SamplerAddressMode tiling);
  virtual void SetFilter(vk::Filter filter);

  void ReadFrom(Buffer &store) override;

  void WriteTo(Buffer &store) override;

  void Upload() override;

  bool IsUploaded() const override;

  void BeforeDestroy() override;

  void Init(DrawingSubsystem *outer) override;

  static Managed<Texture2D> FromData(
      const Array<unsigned char> &data,
      vk::Extent3D size, vk::Format format,
      vk::Filter filter,
      vk::SamplerAddressMode tiling =
          vk::SamplerAddressMode::eRepeat);

  RFUNCTION()
  static Managed<Texture2D> Construct() { return newManagedObject<Texture2D>(); }
  VENGINE_IMPLEMENT_REFLECTED_INTERFACE(Texture2D)
};

REFLECT_IMPLEMENT(Texture2D)
}
