#pragma once
#include "glm/glm.hpp"
template<typename T>
        struct Vec3
{
public:
    T x;
    T y;
    T z;

    explicit Vec3(T fill);

    Vec3(T inX,T inY,T inZ);

    explicit Vec3(const glm::vec<3,T>& vec);

    operator glm::vec<3,T>() const;
    
    template<typename E>
    Vec3<E> Cast() const;

    Vec3 operator+(const Vec3& other) const;
    Vec3 operator-(const Vec3& other) const;
    Vec3 operator/(const Vec3& other) const;
    Vec3 operator*(const Vec3& other) const;
};

template <typename T>
Vec3<T>::Vec3(T fill) : Vec3(fill,fill,fill)
{
}

template <typename T>
Vec3<T>::Vec3(T inX, T inY, T inZ)
{
    x = inX;
    y = inY;
    z = inZ;
}

template <typename T>
Vec3<T>::Vec3(const glm::vec<3, T>& vec)
{
    x = vec.x;
    y = vec.y;
    z = vec.z;
}

template <typename T>
Vec3<T>::operator glm::vec<3, T>() const
{
    return {x,y,z};
}

template <typename T>
template <typename E>
Vec3<E> Vec3<T>::Cast() const
{
    return Vec3<E>(static_cast<E>(x),static_cast<E>(y),static_cast<E>(z));
}

template <typename T>
Vec3<T> Vec3<T>::operator+(const Vec3& other) const
{
    return {x + other.x,y + other.y,z + other.z};
}

template <typename T>
Vec3<T> Vec3<T>::operator-(const Vec3& other) const
{
    return {x - other.x,y - other.y,z - other.z};
}

template <typename T>
Vec3<T> Vec3<T>::operator/(const Vec3& other) const
{
    return {x / other.x,y / other.y,z / other.z};
}

template <typename T>
Vec3<T> Vec3<T>::operator*(const Vec3& other) const
{
    return {x * other.x,y * other.y,z * other.z};
}