#pragma once
#include "glm.h"

namespace rin
{
    template <typename T = float>
    struct Vec3
    {
        T x;
        T y;
        T z;

        template<std::enable_if_t<std::is_default_constructible_v<T>>* = nullptr>
        Vec3()
        {
            x = {};
            y = {};
            z = {};
        }

        Vec3(const T& data)
        {
            x = data;
            y = data;
            z = data;
        }

        Vec3(const T& inX, const T& inY, const T& inZ)
        {
            x = inX;
            y = inY;
            z = inZ;
        }

        explicit Vec3(const glm::vec<3, T>& vec)
        {
            x = vec.x;
            y = vec.y;
            z = vec.z;
        }

        explicit operator glm::vec<3, T,glm::defaultp>() const
        {
            return glm::vec<3, T,glm::defaultp>{x, y, z};
        }

        template <typename E>
        explicit operator Vec3<E>() const
        {
            return Vec3{static_cast<E>(x), static_cast<E>(y), static_cast<E>(z)};
        }

        Vec3 operator+(Vec3 const& other) const
        {
            return Vec3{x + other.x, y + other.y, z + other.z};
        }

        Vec3 operator-(Vec3 const& other) const
        {
            return Vec3{x - other.x, y - other.y, z - other.z};
        }

        Vec3 operator*(Vec3 const& other) const
        {
            return Vec3{x * other.x, y * other.y, z * other.z};
        }

        Vec3 operator/(Vec3 const& other) const
        {
            return Vec3{x / other.x, y / other.y, z / other.z};
        }
    };
}
