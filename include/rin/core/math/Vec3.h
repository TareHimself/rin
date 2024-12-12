#pragma once
#include <glm/glm.hpp>

namespace rin
{
    template <typename T>
    class Vec3
    {
        T x;
        T y;
        T z;

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

        explicit operator glm::vec<3, T>() const
        {
            return glm::vec<2, T>{x, y, z};
        }

        template <typename E>
        explicit operator Vec3<E>() const
        {
            return Vec3{static_cast<E>(x), static_cast<E>(y), static_cast<E>(z)};
        }

        Vec3 operator+(Vec3 const& other)
        {
            return Vec3{x + other.x, y + other.y, z + other.z};
        }

        Vec3 operator-(Vec3 const& other)
        {
            return Vec3{x - other.x, y - other.y, z - other.z};
        }

        Vec3 operator*(Vec3 const& other)
        {
            return Vec3{x * other.x, y * other.y, z * other.z};
        }

        Vec3 operator/(Vec3 const& other)
        {
            return Vec3{x / other.x, y / other.y, z / other.z};
        }
    };
}
