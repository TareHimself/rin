#pragma once
#include <glm/glm.hpp>

namespace rin
{
    template <typename T>
    class Vec2
    {
    public:
        T x;
        T y;

        Vec2(const T& data)
        {
            x = data;
            y = data;
        }

        Vec2(const T& inX, const T& inY)
        {
            x = inX;
            y = inY;
        }

        explicit Vec2(const glm::vec<2, T>& vec)
        {
            x = vec.x;
            y = vec.y;
        }

        explicit operator glm::vec<2, T>() const
        {
            return glm::vec<2, T>{x, y};
        }

        template <typename E>
        explicit operator Vec2<E>() const
        {
            return Vec2<E>{static_cast<E>(x), static_cast<E>(y)};
        }

        Vec2 operator+(Vec2 const& other)
        {
            return Vec2{x + other.x, y + other.y};
        }

        Vec2 operator-(Vec2 const& other)
        {
            return Vec2{x - other.x, y - other.y};
        }

        Vec2 operator*(Vec2 const& other)
        {
            return Vec2{x * other.x, y * other.y};
        }

        Vec2 operator/(Vec2 const& other)
        {
            return Vec2{x / other.x, y / other.y};
        }
    };
}
