#pragma once
#include "glm/glm.hpp"
template<typename T>
        struct Vec4
{

    T x;
    T y;
    T z;
    T w;

    explicit Vec4(const T& fill);

    Vec4(T inX,T inY,T inZ,T inW);

    explicit Vec4(const glm::vec<4,T>& vec);

    operator glm::vec<4,T>() const;

    template<typename E>
    Vec4<E> Cast() const;

    Vec4 operator+(const Vec4& other) const;
    Vec4 operator-(const Vec4& other) const;
    Vec4 operator/(const Vec4& other) const;
    Vec4 operator*(const Vec4& other) const;
};

template <typename T>
Vec4<T>::Vec4(const T& fill) : Vec4(fill,fill,fill,fill)
{
}

template <typename T>
Vec4<T>::Vec4(T inX, T inY, T inZ, T inW)
{
    x = inX;
    y = inY;
    z = inZ;
    w = inW;
}

template <typename T>
Vec4<T>::Vec4(const glm::vec<4, T>& vec)
{
    x = vec.x;
    y = vec.y;
    z = vec.z;
    w = vec.w;
}

template <typename T>
Vec4<T>::operator glm::vec<4, T>() const
{
    return {x,y,z,w};
}

template <typename T>
template <typename E>
Vec4<E> Vec4<T>::Cast() const
{
    return Vec4<E>(static_cast<E>(x),static_cast<E>(y),static_cast<E>(z),static_cast<E>(w));
}

template <typename T>
Vec4<T> Vec4<T>::operator+(const Vec4& other) const
{
    return {x + other.x,y + other.y,z + other.z,w + other.w};
}

template <typename T>
Vec4<T> Vec4<T>::operator-(const Vec4& other) const
{
    return {x - other.x,y - other.y,z - other.z,w - other.w};
}

template <typename T>
Vec4<T> Vec4<T>::operator/(const Vec4& other) const
{
    return {x / other.x,y / other.y,z / other.z,w / other.w};
}

template <typename T>
Vec4<T> Vec4<T>::operator*(const Vec4& other) const
{
    return {x * other.x,y * other.y,z * other.z,w * other.w};
}
