#pragma once

#include "aerox/containers/Array.hpp"
#include "Vec3.hpp"
#include <ostream>
#include<glm/glm.hpp>
#include<glm/gtc/quaternion.hpp>

namespace aerox::math {

    class Quat {
    public:
        float x = 0;
        float y = 0;
        float z = 0;
        float w = 0;

        Quat() = default;

        Quat(float inX, float inY, float inZ, float inW);

        Quat(float angle,const Vec3<>& axis);
        // Quaternion();
        //
        // Quaternion(const float x_, const float y_, const float z_, const float w_);

        friend std::ostream &operator<<(std::ostream &os, const Quat &other) {
            os << other.x << " " << other.y << " " << other.z << " " << other.w;

            return os;
        }

        Vec3<> operator*(const Vec3<>& other) const;

        Quat operator*(const Quat& other) const;

        [[nodiscard]] Vec3<> Forward() const;

        [[nodiscard]] Vec3<> Right() const;

        [[nodiscard]] Vec3<> Up() const;

        [[nodiscard]] Quat ApplyPitch(float delta) const;

        [[nodiscard]] Quat ApplyRoll(float delta) const;

        [[nodiscard]] Quat ApplyYaw(float delta) const;

        [[nodiscard]] Quat ApplyLocalPitch(float delta) const;

        [[nodiscard]] Quat ApplyLocalRoll(float delta) const;

        [[nodiscard]] Quat ApplyLocalYaw(float delta) const;
    };
}

