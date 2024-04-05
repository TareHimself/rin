#include <aerox/math/Transform.hpp>
#include <aerox/math/constants.hpp>


namespace aerox::math {
    Transform::Transform() : location(VECTOR_ZERO), rotation(QUAT_ZERO), scale(VECTOR_UNIT) {
    }

    Transform::Transform(const glm::mat4 &mat) {
        auto l = glm::vec3(mat[3]);

        location = Vec3<float>(l.x, l.y, l.z);


        auto r = glm::quat(glm::mat3(mat));
        rotation = Quat(r.x, r.y, r.z, r.w);

        auto s = glm::vec3(length(glm::vec3(mat[0])), length(glm::vec3(mat[1])), length(glm::vec3(mat[2])));

        scale = Vec3<float>(s.x, s.y, s.z);
    }

    Transform::Transform(const Vec3<> &_loc, const Quat &_rot, const Vec3<> &_scale) : location(_loc),
                                                                                                 rotation(_rot),
                                                                                                 scale(_scale) {

    }

    Transform Transform::RelativeTo(const Transform &other) const {
        return inverse(other.Matrix()) * this->Matrix();
    }

    glm::mat4 Transform::Matrix() const {
        return glm::scale(GetLocationMatrix() * GetRotationMatrix(), {scale.x, scale.y, scale.z});
    }

    glm::mat4 Transform::GetRotationMatrix() const {
        return glm::mat4_cast(glm::quat{rotation.w,rotation.x,rotation.y,rotation.z});
    }

    glm::mat4 Transform::GetLocationMatrix() const {
        return glm::translate(glm::mat4(1.0f), {location.x, location.y, location.z});
    }
}
