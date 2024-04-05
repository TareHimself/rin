#include <aerox/math/Quat.hpp>
#include <aerox/math/Vec3.hpp>
#include <aerox/math/constants.hpp>


namespace aerox::math {

    Vec3<> Quat::Forward() const {
        return *this * VECTOR_FORWARD;
    }

    Vec3<> Quat::Right() const {
        return *this * VECTOR_RIGHT;
    }

    Vec3<> Quat::Up() const {
        return *this * VECTOR_UP;
    }

    Quat Quat::ApplyPitch(float delta) const {
        return *this * Quat(delta, VECTOR_RIGHT);
    }

    Quat Quat::ApplyRoll(float delta) const {
        return *this * Quat(delta, VECTOR_FORWARD);
    }

    Quat Quat::ApplyYaw(float delta) const {
        return *this * Quat(delta, VECTOR_UP);
    }

    Quat Quat::ApplyLocalPitch(float delta) const {
        return *this * Quat(delta, Right());
    }

    Quat Quat::ApplyLocalRoll(float delta) const {
        return *this * Quat(delta, Forward());
    }

    Quat Quat::ApplyLocalYaw(float delta) const {
        return *this * Quat(delta,Up());
    }

    Vec3<float> Quat::operator*(const Vec3<> &other) const {
        auto r = glm::quat{w,x,y,z} * glm::vec3{other.x,other.y,other.z};
        return {r.x,r.y,r.z};
    }

    Quat Quat::operator*(const Quat &other) const {
        auto r = glm::quat{w,x,y,z} * glm::quat{other.w,other.x,other.y,other.z};
        return {r.x,r.y,r.z,r.w};
    }

    Quat::Quat(float inX, float inY, float inZ, float inW)  {
        x = inX;
        y = inY;
        z = inZ;
        w = inW;
    }

    Quat::Quat(float angle, const Vec3<> &axis) {
        auto q = glm::angleAxis(glm::radians(angle),glm::vec3{axis.x,axis.y,axis.z});
        x = q.x;
        y = q.y;
        z = q.z;
        w = q.w;
    }
}
