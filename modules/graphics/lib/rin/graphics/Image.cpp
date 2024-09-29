#include "rin/graphics/Image.hpp"
#define STB_IMAGE_WRITE_IMPLEMENTATION
#define STB_IMAGE_IMPLEMENTATION
#include <stb/stb_image_write.h>
#include <stb/stb_image.h>

template <>
Image<float> Image<float>::LoadFile(const std::filesystem::path& filePath)
{
    int width;
    int height;
    int channels;
    
    auto ptr = stbi_loadf(filePath.string().c_str(),&width,&height,&channels,0);
    auto img = Image{static_cast<unsigned int>(width),static_cast<unsigned int>(height),static_cast<unsigned int>(channels),ptr};
    stbi_image_free(ptr);

    return img;
}


template <typename T>
unsigned int Image<T>::Size()
{
    return _width * _height * _channels;
}

template <>
Image<unsigned char> Image<unsigned char>::LoadFile(const std::filesystem::path& filePath)
{
    int width;
    int height;
    int channels;
    
    auto ptr = stbi_load(filePath.string().c_str(),&width,&height,&channels,0);
    auto img = Image{static_cast<unsigned int>(width),static_cast<unsigned int>(height),static_cast<unsigned int>(channels),ptr};
    stbi_image_free(ptr);

    return img;
}
