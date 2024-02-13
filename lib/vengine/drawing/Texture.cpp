#include <vengine/drawing/Texture.hpp>
#include <vengine/drawing/DrawingSubsystem.hpp>

namespace vengine::drawing {

void Texture::MakeSampler() {
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

vk::Extent3D Texture::GetSize() const {
  return _size;
}

Ref<AllocatedImage> Texture::GetGpuData() const {
  return _gpuData;
}

vk::Sampler Texture::GetSampler() const {
  return _sampler;
}

void Texture::SetMipMapped(const bool newMipMapped) {
  _mipMapped = newMipMapped;
}

bool Texture::IsMipMapped() const {
  return _mipMapped;
}

void Texture::SetGpuData(const Managed<AllocatedImage> &allocation) {
  _gpuData = allocation;
}

void Texture::SetTiling(const vk::SamplerAddressMode tiling) {
  _tiling = tiling;
  MakeSampler();
}

void Texture::SetFilter(const vk::Filter filter) {
  _filter = filter;
  MakeSampler();
}

void Texture::ReadFrom(Buffer &store) {

  store >> _size.width;
  store >> _size.height;
  store >> _size.depth;
  store >> _format;
  store >> _filter;
  store >> _data;
}

void Texture::WriteTo(Buffer &store) {
  store << _size.width;
  store << _size.height;
  store << _size.depth;
  store << _format;
  store << _filter;
  store << _data;
}


void Texture::Upload() {
  if(!IsUploaded()) {
    _gpuData = GetOuter()->CreateImage(_data.data(),_size,_format,vk::ImageUsageFlagBits::eSampled,_mipMapped,_filter);
  }
}

bool Texture::IsUploaded() const {
  return _gpuData;
}

void Texture::BeforeDestroy() {
  Object<DrawingSubsystem>::BeforeDestroy();
  const auto drawer = GetOuter();
  drawer->WaitDeviceIdle();
  drawer->GetDevice().destroySampler(GetSampler());
  _gpuData.Clear();
}

void Texture::Init(DrawingSubsystem * outer) {
  Object<DrawingSubsystem>::Init(outer);
  MakeSampler();
}

Managed<Texture> Texture::FromData(DrawingSubsystem * drawer, const Array<unsigned char> &data, const vk::Extent3D size, const vk::Format format,
                            const vk::Filter filter, const vk::SamplerAddressMode tiling) {
  auto tex = newManagedObject<Texture>();
  tex->_data = data;
  tex->_size = size;
  tex->_format = format;
  tex->_filter = filter;
  tex->_tiling = tiling;
  tex->Init(drawer);
  return tex;
}
}