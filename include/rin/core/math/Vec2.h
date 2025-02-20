#pragma once
#include "glm.h"

namespace rin
{
    template <typename T = float>
    struct Vec2
    {
        T x;
        T y;

        template<std::enable_if_t<std::is_default_constructible_v<T>>* = nullptr>
        Vec2()
        {
            x = {};
            y = {};
        }

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


        // Vec2 operator+(Vec2 const& a,Vec2 const& b) const
        // {
        //     return Vec2{a.x + b.x, a.y + b.y};
        // }
        //
        // Vec2 operator-(Vec2 const& a,Vec2 const& b) const
        // {
        //     return Vec2{a.x - b.x, a.y - b.y};
        // }
        //
        // Vec2 operator*(Vec2 const& a,Vec2 const& b) const
        // {
        //     return Vec2{a.x * b.x, a.y * b.y};
        // }
        //
        // Vec2 operator/(Vec2 const& a,Vec2 const& b) const
        // {
        //     return Vec2{a.x / b.x, a.y / b.y};
        // }
        Vec2 operator+(Vec2 const& other) const
        {
            return Vec2{x + other.x, y + other.y};
        }

        Vec2 operator-(Vec2 const& other) const
        {
            return Vec2{x - other.x, y - other.y};
        }

        Vec2 operator*(Vec2 const& other) const
        {
            return Vec2{x * other.x, y * other.y};
        }

        Vec2 operator/(Vec2 const& other) const
        {
            return Vec2{x / other.x, y / other.y};
        }

        Vec2 operator+(T const& other) const
        {
            return Vec2{x + other, y + other};
        }

        Vec2 operator-(T const& other) const
        {
            return Vec2{x - other, y - other};
        }

        Vec2 operator*(T const& other) const
        {
            return Vec2{x * other, y * other};
        }

        Vec2 operator/(T const& other) const
        {
            return Vec2{x / other, y / other};
        }
    };
}
