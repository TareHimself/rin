#pragma once

#include <glm/vec2.hpp>
#include <ostream>
#include <vulkan/vulkan.hpp>

namespace aerox::math {
    template<typename T = float>
    class Vec2 {
    public:
        T x;
        T y;

        Vec2() = default;

        template<typename E>
        Vec2(E inAll) {
            x = static_cast<T>(inAll);
            y = static_cast<T>(inAll);
        }

        template<typename X, typename Y>
        Vec2(X inX, Y inY) {
            x = static_cast<T>(inX);
            y = static_cast<T>(inY);
        }

        template<typename E>
        Vec2<E> Cast(){
            return Vec2<E>(x,y);
        }

        bool operator==(const Vec2& other) const{
            return x == other.x && y == other.y;
        }

        Vec2 operator+(const Vec2 &other) const {
            return {x + other.x, y + other.y};
        }

        Vec2 operator-(const Vec2 &other) const {
            return {x - other.x, y - other.y};
        }

        Vec2 operator*(const Vec2 &other) const {
            return {x / other.x, y / other.y};
        }

        Vec2 operator/(const Vec2 &other) const {
            return {x / other.x, y / other.y};
        }

        Vec2 &operator+=(const Vec2 &other) {
            x += other.x;
            y += other.y;
            return *this;
        }

        Vec2 &operator-=(const Vec2 &other) {
            x -= other.x;
            y -= other.y;
            return *this;
        }

        Vec2 &operator*=(const Vec2 &other) {
            x *= other.x;
            y *= other.y;
            return *this;
        }

        Vec2 &operator/=(const Vec2 &other) {
            x /= other.x;
            y /= other.y;
            return *this;
        }

        Vec2 operator+(const T &other) const {
            return {x + other, y + other};
        }

        Vec2 operator-(const T &other) const {
            return {x - other, y - other};
        }

        Vec2 operator*(const T &other) const {
            return {x * other, y * other};
        }

        Vec2 operator/(const T &other) const {
            return {x / other, y / other};
        }

        Vec2 &operator+=(const T &other) {
            x += other;
            y += other;
            return *this;
        }

        Vec2 &operator-=(const T &other) {
            x -= other;
            y -= other;
            return *this;
        }

        Vec2 &operator*=(const T &other) {
            x *= other;
            y *= other;
            return *this;
        }

        Vec2 &operator/=(const T &other) {
            x /= other;
            y /= other;
            return *this;
        }

        double Cross(const Vec2& other){
            return x * other.y - y * other.x;
        }

        explicit operator std::string() const {
            return std::to_string(x) + " " + std::to_string(y);
        }

        friend std::ostream &operator<<(std::ostream &os, const Vec2 &other) {
            os << static_cast<std::string>(other);

            return os;
        }
    };
}
