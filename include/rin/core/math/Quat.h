#pragma once
#include "glm.h"
#include "Vec3.h"


namespace rin
{
    
    struct Quat
    {
        float w;
        float x;
        float y;
        float z;

        Quat() = default;
        
        Quat(const float& data)
        {
            w = data;
            x = data;
            y = data;
            z = data;
        }

        Quat(const float& angle,const Vec3<>& axis) : Quat(glm::angleAxis(angle,static_cast<glm::vec3>(axis)))
        {
 
        }

        Quat(const float& inW,const float& inX, const float& inY, const float& inZ)
        {
            w = inW;
            x = inX;
            y = inY;
            z = inZ;
        }

        explicit Quat(const glm::quat& quat)
        {
            w = quat.w;
            x = quat.x;
            y = quat.y;
            z = quat.z;
        }

        explicit operator glm::quat() const
        {
            return glm::quat{w,x, y, z};
        }
        
        Quat operator*(const Quat& other) const
        {
            return static_cast<Quat>(static_cast<glm::quat>(*this) * static_cast<glm::quat>(other));
        }
        
        // Quat operator+(Quat const& other) const
        // {
        //     return Quat{w + other.w,x + other.x, y + other.y, z + other.z};
        // }
        //
        // Quat operator-(Quat const& other) const
        // {
        //     return Quat{w - other.w,x - other.x, y - other.y, z - other.z};
        // }
        //
        // Quat operator*(Quat const& other) const
        // {
        //     return Quat{w * other.w,x * other.x, y * other.y, z * other.z };
        // }
        //
        // Quat operator/(Quat const& other) const
        // {
        //     return Quat{w / other.w,x / other.x, y / other.y, z / other.z};
        // }
        //
        // Quat operator+(float const& other) const
        // {
        //     return Quat{w + other, x + other, y + other, z + other};
        // }
        //
        // Quat operator-(float const& other) const
        // {
        //     return Quat{w - other, x - other, y - other, z - other};
        // }
        //
        // Quat operator*(float const& other) const
        // {
        //     return Quat{w * other,x * other, y * other, z * other};
        // }
        //
        // Quat operator/(float const& other) const
        // {
        //     return Quat{w / other,x / other, y / other, z / other};
        // }
    };
}
