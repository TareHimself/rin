#pragma once
#include <xutility>
#include <filesystem>

template<typename T>
class Image
{
    T * _data = nullptr;
    unsigned int _width;
    unsigned int _height;
    unsigned int _channels;
    bool _owned = false;
public:

    Image(unsigned int width,unsigned int height, unsigned int channels);
    Image(unsigned int width,unsigned int height, unsigned int channels,T * data,bool copy = true);
    ~Image();

    unsigned int Size();
    template<typename E>
    Image<E> Cast() const;

    template<typename E>
    Image<E> Cast() const;
    
    static Image LoadFile(const std::filesystem::path& filePath);

    Image SavePng(const std::filesystem::path& filePath);
    Image SaveJpeg(const std::filesystem::path& filePath);
};

template <typename T>
Image<T>::Image(unsigned int width, unsigned int height, unsigned int channels)
{
    _data = new T[width * height * channels];
    _width = width;
    _height = height;
    _channels = channels;
}

template <typename T>
Image<T>::Image(unsigned int width, unsigned int height, unsigned int channels, T* data, bool copy)
{
    _width = width;
    _height = height;
    _channels = channels;
    _owned = copy;
    
    if(copy)
    {
        _data = new T[width * height * channels];
        std::copy_n(data,width * height * channels * sizeof(T),_data);
    }
    else
    {
        _data = data;
    }
}

template <typename T>
Image<T>::~Image()
{
    if(_owned)
    {
        delete[] _data;
    }
}

template <typename T>
template <typename E>
Image<E> Image<T>::Cast() const
{
    auto newArr = new E[_width * _height * _channels];
    for(auto i = 0; )
}

