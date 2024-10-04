#define STB_IMAGE_WRITE_IMPLEMENTATION
#define STB_IMAGE_IMPLEMENTATION
#include "rin/graphics/Image.hpp"
#include <stb/stb_image_write.h>
#include <stb/stb_image.h>

template <>
Image<float> Image<float>::LoadFile(const std::filesystem::path& filePath)
{
    int width;
    int height;
    int channels;
    
    if(auto ptr = stbi_loadf(filePath.string().c_str(),&width,&height,&channels,0))
    {
        auto img = Image{width,height,channels,ptr};
        stbi_image_free(ptr);

        return img;
    }
    return Image{};
}

template <>
Image<unsigned char> Image<unsigned char>::LoadFile(const std::filesystem::path& filePath)
{
    int width;
    int height;
    int channels;
    
    if(auto ptr = stbi_load(filePath.string().c_str(),&width,&height,&channels,0))
    {
        auto img = Image{width,height,channels,ptr};
        stbi_image_free(ptr);

        return img;
    }
    return Image{};
}


template <>
void Image<unsigned char>::SavePng(const std::filesystem::path& filePath) const
{
    stbi_write_png(filePath.string().c_str(),GetWidth(),GetHeight(),GetChannels(),GetData(),GetWidth() * GetChannels());
}

template <>
void Image<unsigned char>::SaveJpeg(const std::filesystem::path& filePath,int quality) const
{
    stbi_write_jpg(filePath.string().c_str(),GetWidth(),GetHeight(),GetChannels(),GetData(),quality);
}



