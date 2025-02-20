#pragma once
#include "glm.h"

namespace rin
{
    template <typename T = float>
    struct Vec4
    {
        T x;
        T y;
        T z;
        T w;

        template<std::enable_if_t<std::is_default_constructible_v<T>>* = nullptr>
        Vec4()
        {
            x = {};
            y = {};
            z = {};
            w = {};
        }

        Vec4(const T& data)
        {
            x = data;
            y = data;
            z = data;
            w = data;
        }

        Vec4(const T& inX, const T& inY, const T& inZ, const T& inW)
        {
            x = inX;
            y = inY;
            z = inZ;
            w = inW;
        }

        explicit Vec4(const glm::vec<4, T>& vec)
        {
            x = vec.x;
            y = vec.y;
            z = vec.z;
            w = vec.w;
        }

        explicit operator glm::vec<4, T>() const
        {
            return glm::vec<4, T>{x, y, z, w};
        }

        template <typename E>
        explicit operator Vec4<E>() const
        {
            return Vec4{static_cast<E>(x), static_cast<E>(y), static_cast<E>(z), static_cast<E>(w)};
        }

        Vec4 operator+(Vec4 const& other) const
        {
            return Vec4{x + other.x, y + other.y, z + other.z, w + other.w};
        }

        Vec4 operator-(Vec4 const& other) const
        {
            return Vec4{x - other.x, y - other.y, z - other.z, w - other.w};
        }

        Vec4 operator*(Vec4 const& other) const
        {
            return Vec4{x * other.x, y * other.y, z * other.z, w * other.w};
        }

        Vec4 operator/(Vec4 const& other) const
        {
            return Vec4{x / other.x, y / other.y, z / other.z, w / other.w};
        }

        Vec4 operator+(T const& other) const
        {
            return Vec4{x + other, y + other, z + other, w + other};
        }

        Vec4 operator-(T const& other) const
        {
            return Vec4{x - other, y - other, z - other, w - other};
        }

        Vec4 operator*(T const& other) const
        {
            return Vec4{x * other, y * other.y, z * other, w * other};
        }

        Vec4 operator/(T const& other) const
        {
            return Vec4{x / other, y / other, z / other, w / other};
        }
    };
}
