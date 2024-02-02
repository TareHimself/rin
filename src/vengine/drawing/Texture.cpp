#include <vengine/drawing/Texture.hpp>
#include <vengine/drawing/Drawer.hpp>

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

vk::Extent3D Texture::GetSize() const {
  return _size;
}

WeakPointer<AllocatedImage> Texture::GetGpuData() const {
  return _gpuData;
}

vk::Sampler Texture::GetSampler() const {
  return _sampler;
}

void Texture::SetGpuData(const Pointer<AllocatedImage> &allocation) {
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
  return _gpuData;
}

void Texture::HandleDestroy() {
  Object<Drawer>::HandleDestroy();
  const auto drawer = GetOuter();
  drawer->GetDevice().waitIdle();
  drawer->GetDevice().destroySampler(GetSampler());
  _gpuData.Clear();
}

void Texture::Init(Drawer * outer) {
  Object<Drawer>::Init(outer);
  MakeSampler();
}

Pointer<Texture> Texture::FromData(Drawer * drawer, const Array<unsigned char> &data, const vk::Extent3D size, const vk::Format format,
                            const vk::Filter filter, const vk::SamplerAddressMode tiling) {
  auto tex = newSharedObject<Texture>();
  tex->_data = data;
  tex->_size = size;
  tex->_format = format;
  tex->_filter = filter;
  tex->_tiling = tiling;
  tex->Init(drawer);
  return tex;
}
}
