#include "Texture.hpp"

#include "Drawer.hpp"

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
  _sampler = GetOuter()->GetDevice().createSampler({{},_filter,_filter});
}

std::optional<AllocatedImage> Texture::GetGpuData() const {
  return _gpuData;
}

vk::Sampler Texture::GetSampler() const {
  return _sampler;
}

void Texture::SetGpuData(const std::optional<AllocatedImage> &allocation) {
  _gpuData = allocation;
}

void Texture::SetTiling(vk::SamplerAddressMode tiling) {
  _tiling = tiling;
  MakeSampler();
}

void Texture::SetFilter(vk::Filter filter) {
  _filter = filter;
  MakeSampler();
}

String Texture::GetSerializeId() {
  return "TEXTURE";
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
    _gpuData = GetOuter()->CreateImage(_data.data(),_size,_format,vk::ImageUsageFlagBits::eSampled);
  }
}

bool Texture::IsUploaded() const {
  return _gpuData.has_value();
}

void Texture::HandleDestroy() {
  Object<Drawer>::HandleDestroy();
  if(IsUploaded()) {
    const auto data = _gpuData.value();
    GetOuter()->GetDevice().waitIdle();
    GetOuter()->GetAllocator()->DestroyImage(data);
    _gpuData.reset();
  }
  GetOuter()->GetDevice().destroySampler(GetSampler());
}

void Texture::Init(Drawer *outer) {
  Object<Drawer>::Init(outer);
  MakeSampler();
}

Texture * Texture::FromData(Drawer * drawer, const Array<unsigned char> &data, const vk::Extent3D size, const vk::Format format,
                            const vk::Filter filter,vk::SamplerAddressMode tiling) {
  const auto tex = newObject<Texture>();
  tex->_data = data;
  tex->_size = size;
  tex->_format = format;
  tex->_filter = filter;
  tex->_tiling = tiling;
  tex->Init(drawer);
  return tex;
}
}
