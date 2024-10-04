#pragma once
#include <xutility>
#include <filesystem>
#include <functional>

#include "rin/core/math/Vec2.hpp"

template<typename T>
class Image
{
    T * _data = nullptr;
    unsigned int _width;
    unsigned int _height;
    unsigned int _channels;
    bool _owned;
public:

    Image()
    {
        _data = nullptr;
        _width = 0;
        _height = 0;
        _channels = 0;
        _owned = true;
    }
    
    Image(int width,int height, int channels)
    {
        _width = width < 0 ? 0 : width;
        _height = height < 0 ? 0 : height;
        _channels = channels < 0 ? 0 : channels;
        _data = new T[GetElementCount()]();
        _owned = true;
    }
    
    Image(int width,int height, int channels,T * data,bool owned = false)
    {
        _width = width < 0 ? 0 : width;
        _height = height < 0 ? 0 : height;
        _channels = channels < 0 ? 0 : channels;
        _owned = owned;
    
        if(owned)
        {
            _data = data;
        }
        else
        {
            const auto size = GetElementCount();
            _data = new T[size]();
            std::copy_n(data,size * sizeof(T),_data);
        }
    }

    Image(Image& other)
    {
        if(_data && _owned)
        {
            delete[] _data;
            _data = nullptr;
        }
        
        _width = other._width;
        _height = other._height;
        _channels = other._channels;
        const auto size = GetElementCount();
        _data = new T[size]();
        std::copy_n(other._data,size * sizeof(T),_data);
        _owned = true;
    }
    
    ~Image()
    {
        if(_data && _owned)
        {
            delete[] _data;
        }
    }

    T* GetData()
    {
        return _data;
    }

    T* GetData() const
    {
        return _data;
    }

    int GetWidth() const
    {
        return _width;
    }

    int GetHeight() const
    {
        return _height;
    }

    int GetChannels() const
    {
        return _channels;
    }
    
    Vec2<int> GetSize() const
    {
        return {GetWidth(),GetHeight()};
    }

    long GetElementCount() const
    {
        return static_cast<long>(GetWidth()) * static_cast<long>(GetHeight()) * static_cast<long>(GetChannels());
    }
    
    T& operator()(unsigned int x,unsigned int y,unsigned int c)
    {
        return At(x,y,c);
    }
    
    T operator()(unsigned int x,unsigned int y,unsigned int c) const
    {
        return At(x,y,c);
    }

    T& At(unsigned int x,unsigned int y,unsigned int c)
    {
        return _data[y * (_width * _channels) + (x * _channels + c)];
    }

    T At(unsigned int x,unsigned int y,unsigned int c) const
    {
        return _data[y * (_width * _channels) + (x * _channels + c)]; 
    }

    T& At(unsigned int index)
    {
        return _data[index];
    }

    T At(unsigned int index) const
    {
        return _data[index]; 
    }
    
    template<typename E>
    Image<E> Cast() const
    {
        auto size = GetElementCount();
        auto newArr = new E[size];
        for(int i = 0; i < size; ++i)
        {
            newArr[i] = static_cast<E>(this->At(i));
        }
        return Image<E>{GetWidth(),GetHeight(),GetChannels(),newArr,true};
    }

    template<typename E>
    Image<E> Cast(const std::function<E(const T&)>& transform) const
    {
        auto size = GetElementCount();
        auto newArr = new E[size];
        for(int i = 0; i < size; ++i)
        {
            newArr[i] = transform(this->At(i));
        }
        return Image<E>{GetWidth(),GetHeight(),GetChannels(),newArr,true};
    }

    static Image LoadFile(const std::filesystem::path& filePath);

    void SavePng(const std::filesystem::path& filePath) const;
    void SaveJpeg(const std::filesystem::path& filePath,int quality = 100) const;

    void SetChannels(int&& channels,T&& fillValue = 255)
    {
        if(_channels == channels) return;
        auto oldChannels = _channels;
        const auto newChannels = channels;

        _channels = newChannels;

        if(!_data) return;
        
        auto newData = new T[_width * _height * _channels]();

        auto min = newChannels > oldChannels ? oldChannels : newChannels;
        
        for(auto y = 0; y < _height; y++)
        {
            for(auto x = 0; x < _width; x++)
            {
                for(auto c = 0; c < newChannels; c++)
                {
                    auto i = (y * (_width * newChannels)) + (x * newChannels) + c;
                    auto j = (y * (_width * oldChannels)) + (x * oldChannels) + c;
                    auto newVal = c >= min ? fillValue : _data[j];
                    newData[i] = newVal;
                }
            }
        }

        if(_owned)
        {
            delete[] _data;
        }

        _data = newData;
    }

    bool CopyTo(Image& other,const Vec2<int>& srcBegin,const Vec2<int>& srcEnd,const Vec2<int>& dstBegin = {0,0})
    {
        auto srcRowSize = GetWidth() * GetChannels();
        auto dstRowSize = other.GetWidth() * other.GetChannels();
        if(srcRowSize != dstRowSize)
        {
            return false;
        }
        
        auto deltaY = srcEnd.y - srcBegin.y;
        auto deltaX = (srcEnd.x - srcBegin.x) * GetChannels();
        for(auto i = 0; i < deltaY; i++)
        {
            auto srcStartN = ((srcBegin.y + i) * srcRowSize) + srcBegin.x;
            auto dstStartN = ((dstBegin.y + i) * dstRowSize) + dstBegin.x;
            std::copy_n(GetData() + srcStartN,deltaX,other.GetData() + dstStartN);
        }

        return true;
    }
};


