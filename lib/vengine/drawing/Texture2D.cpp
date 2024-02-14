#include "vengine/Engine.hpp"

#include <vengine/drawing/Texture2D.hpp>
#include <vengine/drawing/DrawingSubsystem.hpp>

namespace vengine::drawing {

void Texture2D::MakeSampler() {
  if(_sampler) {
    GetOuter()->GetDevice().destroySampler(_sampler);
    _sampler = nullptr;
  }
  vk::SamplerCreateInfo samplerInfo{};
  samplerInfo.setMagFilter(_filter);
  samplerInfo.setMinFilter(_filter);
  samplerInfo.setAddressModeU(_tiling);
  samplerInfo.setAddressModeV(_tiling);
  samplerInfo.setAddressModeW(_tiling);
  samplerInfo.setMipmapMode(vk::SamplerMipmapMode::eNearest);
  samplerInfo.setAnisotropyEnable(true);
  _sampler = GetOuter()->GetDevice().createSampler({{},_filter,_filter});
}

vk::Extent3D Texture2D::GetSize() const {
  return _size;
}

Ref<AllocatedImage> Texture2D::GetGpuData() const {
  return _gpuData;
}

vk::Sampler Texture2D::GetSampler() const {
  return _sampler;
}

void Texture2D::SetMipMapped(const bool newMipMapped) {
  _mipMapped = newMipMapped;
}

bool Texture2D::IsMipMapped() const {
  return _mipMapped;
}

void Texture2D::SetGpuData(const Managed<AllocatedImage> &allocation) {
  _gpuData = allocation;
}

void Texture2D::SetTiling(const vk::SamplerAddressMode tiling) {
  _tiling = tiling;
  MakeSampler();
}

void Texture2D::SetFilter(const vk::Filter filter) {
  _filter = filter;
  MakeSampler();
}

void Texture2D::ReadFrom(Buffer &store) {

  store >> _size.width;
  store >> _size.height;
  store >> _size.depth;
  store >> _format;
  store >> _filter;
  store >> _data;
}

void Texture2D::WriteTo(Buffer &store) {
  store << _size.width;
  store << _size.height;
  store << _size.depth;
  store << _format;
  store << _filter;
  store << _data;
}


void Texture2D::Upload() {
  if(!IsUploaded()) {
    _gpuData = GetOuter()->CreateImage(_data.data(),_size,_format,vk::ImageUsageFlagBits::eSampled,_mipMapped,_filter);
  }
}

bool Texture2D::IsUploaded() const {
  return _gpuData;
}

void Texture2D::BeforeDestroy() {
  Object<DrawingSubsystem>::BeforeDestroy();
  const auto drawer = GetOuter();
  drawer->WaitDeviceIdle();
  drawer->GetDevice().destroySampler(GetSampler());
  _gpuData.Clear();
}

void Texture2D::Init(DrawingSubsystem * outer) {
  Object<DrawingSubsystem>::Init(outer);
  MakeSampler();
}

Managed<Texture2D> Texture2D::FromData(const Array<unsigned char> &data, const vk::Extent3D size, const vk::Format format,
                                   const vk::Filter filter, const vk::SamplerAddressMode tiling) {
  auto drawer = Engine::Get()->GetDrawingSubsystem().Reserve();
  auto tex = newManagedObject<Texture2D>();
  tex->_data = data;
  tex->_size = size;
  tex->_format = format;
  tex->_filter = filter;
  tex->_tiling = tiling;
  tex->Init(drawer.Get());
  return tex;
}
}
