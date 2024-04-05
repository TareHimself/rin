#define STB_IMAGE_WRITE_IMPLEMENTATION
#include "aerox/Engine.hpp"
#include <aerox/drawing/Texture.hpp>
#include <aerox/drawing/DrawingSubsystem.hpp>
#include <stb_image_write.h>

namespace aerox::drawing {

void Texture::MakeSampler() {
  auto drawer = Engine::Get()->GetDrawingSubsystem().lock();
  if(_sampler) {
    drawer->GetVirtualDevice().destroySampler(_sampler);
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
  _sampler = drawer->GetVirtualDevice().createSampler({{},_filter,_filter});
}

vk::Extent3D Texture::GetSize() const {
  return _size;
}

std::weak_ptr<AllocatedImage> Texture::GetGpuData() const {
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

void Texture::SetGpuData(const std::shared_ptr<AllocatedImage> &allocation) {
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
    _gpuData = Engine::Get()->GetDrawingSubsystem().lock()->CreateImage(_data.data(),_size,_format,vk::ImageUsageFlagBits::eSampled,_mipMapped,_filter,fmt::format("Texture : {}x{}",_size.width,_size.height));
  }
}

bool Texture::IsUploaded() const {
  return static_cast<bool>(_gpuData);
}

void Texture::OnDestroy() {
  Object::OnDestroy();
  GetOwner()->WaitDeviceIdle();
  GetOwner()->GetVirtualDevice().destroySampler(GetSampler());
  _gpuData.reset();
}

void Texture::OnInit(DrawingSubsystem * subsystem) {
  TOwnedBy::OnInit(subsystem);
  MakeSampler();
}

bool Texture::Save(const fs::path &path) {
  if(_data.empty()) {
    return false;
  }

  const std::vector<unsigned char> dataToSave = _data;

  const auto channels = Texture::GetFormatChannels(_format);

  return stbi_write_png(path.string().c_str(),_size.width,_size.height,channels,dataToSave.data(),_size.width * channels);
}

uint32_t Texture::GetFormatChannels(const vk::Format &format) {
  switch (format) {
  case vk::Format::eR8G8B8Unorm:
    return 3;
  case vk::Format::eR8G8B8A8Unorm:
    return 4;
  default:
    return 4;
  }
}

std::shared_ptr<Texture> Texture::FromMemory(const std::vector<unsigned char> &data, const vk::Extent3D size, const vk::Format format,
                                     const vk::Filter filter, const vk::SamplerAddressMode tiling) {
  auto tex = newObject<Texture>();
  tex->_data = data;
  tex->_size = size;
  tex->_format = format;
  tex->_filter = filter;
  tex->_tiling = tiling;
  tex->Init(Engine::Get()->GetDrawingSubsystem().lock().get());
  return tex;
}

std::shared_ptr<Texture> Texture::FromAllocated(const std::shared_ptr<AllocatedImage> &image,vk::Filter filter, vk::SamplerAddressMode tiling) {
  auto tex = newObject<Texture>();
  tex->_data = {};
  tex->_size = image->extent;
  tex->_format = image->format;
  tex->_filter = filter;
  tex->_tiling = tiling;
  tex->Init(Engine::Get()->GetDrawingSubsystem().lock().get());
  return tex;
}

std::weak_ptr<AllocatedImage> Texture::GetGpuData() {
  return _gpuData;
}
}
