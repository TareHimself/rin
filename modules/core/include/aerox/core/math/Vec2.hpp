#pragma once
#include <glm_1.0.0/include/glm/glm.hpp> // This include needs to be investigated. for some reason glm/include/* does not work
#include "utils.hpp"
#include <algorithm>
namespace aerox
{
    template<typename T>
        struct Vec2
    {
    public:
        T x;
        T y;


    public:
        explicit Vec2(T fill);

        Vec2(T inX,T inY);

        explicit Vec2(const glm::vec<2,T>& vec);

        operator glm::vec<2,T>() const;
        
        template<typename E>
        Vec2<E> Cast() const;



        Vec2 operator+(const Vec2& other) const;
        Vec2 operator-(const Vec2& other) const;
        Vec2 operator/(const Vec2& other) const;
        Vec2 operator*(const Vec2& other) const;

        Vec2 operator+(const T& other) const;
        Vec2 operator-(const T& other) const;
        Vec2 operator/(const T& other) const;
        Vec2 operator*(const T& other) const;


        T Max();

        Vec2 Abs();

        T Dot(const Vec2& other);
        
        T Acos(const Vec2& other);

        T Cross(const Vec2& other);

        bool NearlyEquals(const Vec2& other,T tolerance) const;
    };

    template <typename T>
    Vec2<T>::Vec2(T fill) : Vec2(fill,fill)
    {

    }

    template <typename T>
    Vec2<T>::Vec2(T inX, T inY)
    {
        x = inX;
        y = inY;
    }

    template <typename T>
    Vec2<T>::Vec2(const glm::vec<2, T>& vec)
    {
        x = vec.x;
        y = vec.y;
    }

    template <typename T>
    Vec2<T>::operator glm::vec<2, T>() const
    {
        return {x,y};
    }

    template <typename T>
    template <typename E>
    Vec2<E> Vec2<T>::Cast() const
    {
        return Vec2<E>(static_cast<E>(x),static_cast<E>(y));
    }

    template <typename T>
    Vec2<T> Vec2<T>::operator+(const Vec2& other)  const
    {
        return Vec2{x + other.x,y + other.y};
    }

    template <typename T>
    Vec2<T> Vec2<T>::operator-(const Vec2& other)  const
    {
        return Vec2{x - other.x,y - other.y};
    }

    template <typename T>
    Vec2<T> Vec2<T>::operator/(const Vec2& other)  const
    {
        return Vec2{x / other.x,y / other.y};
    }

    template <typename T>
    Vec2<T> Vec2<T>::operator*(const Vec2& other)  const
    {
        return Vec2{x * other.x,y * other.y};
    }

    template <typename T>
    Vec2<T> Vec2<T>::operator+(const T& other)  const
    {
        return Vec2{x + other,y + other};
    }

    template <typename T>
    Vec2<T> Vec2<T>::operator-(const T& other)  const
    {
        return Vec2{x - other,y - other};
    }

    template <typename T>
    Vec2<T> Vec2<T>::operator/(const T& other)  const
    {
        return Vec2{x / other,y / other};
    }

    template <typename T>
    Vec2<T> Vec2<T>::operator*(const T& other)  const
    {
        return Vec2{x * other,y * other};
    }

    template <typename T>
    T Vec2<T>::Max()
    {
        return std::max(x,y);
    }

    template <typename T>
    Vec2<T> Vec2<T>::Abs()
    {
        return Vec2{abs(x),abs(y)};
    }

    template <typename T>
    T Vec2<T>::Dot(const Vec2& other)
    {
        auto ax = x, ay = y, bx = other.x, by = other.y;
        return ax * bx + ay * by;
    }

    template <typename T>
    T Vec2<T>::Acos(const Vec2& other)
    {
        return {};//Vec2{glm::acos(*this,other)};
    }

    template <typename T>
    T Vec2<T>::Cross(const Vec2& other)
    {
        auto ux = x,uy = y,vx = other.x,vy = other.y;
        return ux * vy - uy * vx;
    }

    template <typename T>
    bool Vec2<T>::NearlyEquals(const Vec2& other,T tolerance) const
    {
        return (*this - other).Abs().Max() < tolerance;
    }
}

