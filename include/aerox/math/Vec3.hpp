#pragma once

#include "Vec2.hpp"

#include <ostream>
#include <glm/glm.hpp>
#include <glm/ext/matrix_transform.hpp>


namespace aerox::math {

    template<typename T = float>
    class Vec3 {

    public:
        T x;
        T y;
        T z;

        Vec3() = default;

        template<typename E>
        Vec3(E inAll){
            x = static_cast<T>(inAll);
            y = static_cast<T>(inAll);
            z = static_cast<T>(inAll);
        }

        template<typename X,typename Y,typename Z>
        Vec3(X inX,Y inY,Z inZ){
            x = static_cast<T>(inX);
            y = static_cast<T>(inY);
            z = static_cast<T>(inZ);
        }

        template<typename E>
        Vec2<E> Cast(){
            return Vec2<E>(x,y,z);
        }

        bool operator==(const Vec3& other) const{
            return x == other.x && y == other.y && z == other.z;
        }

        Vec3 operator+(const Vec3& other) const {
            return {x + other.x,y + other.y,z + other.z};
        }
        Vec3 operator-(const Vec3& other) const {
            return {x - other.x,y - other.y,z - other.z};
        }
        Vec3 operator*(const Vec3& other) const {
            return {x / other.x,y / other.y,z / other.z};
        }
        Vec3 operator/(const Vec3& other) const {
            return {x / other.x,y / other.y,z / other.z};
        }

        Vec3& operator+=(const Vec3& other){
            x += other.x;
            y += other.y;
            z += other.z;
            return *this;
        }
        Vec3& operator-=(const Vec3& other){
            x -= other.x;
            y -= other.y;
            z -= other.z;
            return *this;
        }
        Vec3& operator*=(const Vec3& other){
            x *= other.x;
            y *= other.y;
            z *= other.z;
            return *this;
        }
        Vec3& operator/=(const Vec3& other){
            x /= other.x;
            y /= other.y;
            z /= other.z;
            return *this;
        }

        Vec3 operator+(const T& other) const {
            return {x + other,y + other,z + other};
        }
        Vec3 operator-(const T& other) const {
            return {x - other,y - other,z - other};
        }
        Vec3 operator*(const T& other) const {
            return {x * other,y * other,z * other};
        }
        Vec3 operator/(const T& other) const {
            return {x / other,y / other,z / other};
        }

        Vec3& operator+=(const T& other){
            x += other;
            y += other;
            z += other;
            return *this;
        }
        Vec3& operator-=(const T& other){
            x -= other;
            y -= other;
            z -= other;
            return *this;
        }
        Vec3& operator*=(const T& other){
            x *= other;
            y *= other;
            z *= other;
            return *this;
        }
        Vec3& operator/=(const T& other){
            x /= other;
            y /= other;
            z /= other;
            return *this;
        }

        explicit operator std::string() const{
            return std::to_string(x) + " " + std::to_string(y) + " " + std::to_string(z);
        }

        friend std::ostream &operator<<(std::ostream &os, const Vec3 &other) {
            os << static_cast<std::string>(other);

            return os;
        }
    };

}
