#pragma once
#include <vector>

namespace aerox::assets {
template <typename T = unsigned int>
class Image {
  std::vector<T> _pixels;
  uint32_t _width;
  uint32_t _height;
  uint32_t _components;
public:
  Image(uint32_t width, uint32_t height,uint32_t components,const std::optional<std::vector<T>>&data = {});

  Image(uint32_t width, uint32_t height,uint32_t components,const T * data);

  std::vector<T> GetPixels() const;

  uint32_t GetWidth() const;

  uint32_t GetHeight() const;

  T * Get(uint32_t x, uint32_t y, uint32_t component);

  T * Get(uint32_t x, uint32_t y, uint32_t component) const;
};

template <typename T> Image<T>::Image(const uint32_t width,
                                                const uint32_t height,uint32_t components,const std::optional<std::vector<T>>&data) {
  
  _width = width;
  _height = height;
  _components = components;
  if(data.has_value()) {
    _pixels = std::vector<T>{data.value().begin(),data.value().end()};
  } else {
    _pixels.resize(_width * _height * _components);
  }
}

template <typename T> Image<T>::Image(uint32_t width, uint32_t height,uint32_t components,
    const T *data) {
  _width = width;
  _height = height;
  _components = components;
  _pixels.resize(_width * _height * _components);
  memcpy(_pixels.data(), data, _pixels.size());
}

template <typename T> std::vector<T> Image<T>::GetPixels() const {
  return _pixels;
}

template <typename T> uint32_t Image<T>::GetWidth() const {
  return _width;
}

template <typename T> uint32_t Image<T>::GetHeight() const {
  return _height;
}

template <typename T> T *Image<T>::Get(uint32_t x,
  uint32_t y, uint32_t component) {
  const auto idx = y * _width + x * _components;
  return &_pixels.at(idx + component);
}

template <typename T> T *Image<T>::Get(uint32_t x,
  uint32_t y, uint32_t component) const {
  const auto idx = y * _width + x * _components;
  return &_pixels.at(idx + component);
}

//template <typename T> void Image<T>::ReadFrom(Buffer &store) {
//  store >> _width;
//  store >> _height;
//  store >> _components;
//  store >> _pixels;
//}
//
//template <typename T> void Image<T>::WriteTo(Buffer &store) {
//  store << _width;
//  store << _height;
//  store << _components;
//  store << _pixels;
//}


}
