#include "math.hpp"
#define GLM_ENABLE_EXPERIMENTAL
#include <iostream>
#include "glm/ext.hpp"
#include<glm/glm.hpp>
#include<glm/gtc/quaternion.hpp>
#include <glm/gtx/matrix_transform_2d.hpp>

Vector2::operator glm::vec<2, float>()
{
    return {x,y};
}

Vector2::Vector2(glm::vec2 other)
{
    x = other[0];
    y = other[1];
}

Vector3::operator glm::vec<3, float>()
{
    return {x, y, z};
}

Vector3::Vector3(glm::vec3 other)
{
    x = other.x;
    y = other.y;
    z = other.z;
}

Vector4::operator glm::vec<4, float>()
{
    return {x, y, z, w};
}

Vector4::Vector4(glm::vec4 other)
{
    w = other.w;
    x = other.x;
    y = other.y;
    z = other.z;
}

Quaternion::operator glm::qua<float>()
{
    return {w, x, y, z};
}

Quaternion::Quaternion(glm::qua<float> other)
{
    w = other.w;
    x = other.x;
    y = other.y;
    z = other.z;
}

Matrix3::operator glm::mat<3, 3, float>()
{
    return {column1,column2,column3};
}

Matrix3::Matrix3(glm::mat3 other)
{
    column1 = Vector3(other[0]);
    column2 = Vector3(other[1]);
    column3 = Vector3(other[2]);
}

Matrix4::operator glm::mat<4, 4, float>()
{
    return {column1,column2,column3,column4};
}

Matrix4::Matrix4(glm::mat4 other)
{
    column1 = Vector4(other[0]);
    column2 = Vector4(other[1]);
    column3 = Vector4(other[2]);
    column4 = Vector4(other[3]);
}

void mathMultiplyQuatQuat(Quaternion* result, Quaternion* left, Quaternion* right)
{
    *result = static_cast<glm::quat>(*left) * static_cast<glm::quat>(*right);
}

void mathMultiplyQuatVector4(Vector4* result, Quaternion* left, Vector4* b)
{
    *result = static_cast<glm::quat>(*left) * static_cast<glm::vec4>(*b);
}

void mathMultiplyMatrix4Matrix4(Matrix4* result, Matrix4* left, Matrix4* right)
{
    *result = static_cast<glm::mat4>(*left) * static_cast<glm::mat4>(*right);
}

void mathMultiplyMatrix4Vector4(Vector4* result, Matrix4* left, Vector4* right)
{
    *result = static_cast<glm::mat4>(*left) * static_cast<glm::vec4>(*right);
}

void mathInverseMatrix4(Matrix4* result, Matrix4* a)
{
    *result = glm::inverse(static_cast<glm::mat4>(*a));
}

void mathQuatToMatrix4(Matrix4* result, Quaternion* a)
{
    *result = glm::mat4_cast(static_cast<glm::quat>(*a));
}

void mathTranslateMatrix4(Matrix4* result, Matrix4* target, Vector3* translation)
{
    *result = glm::translate(static_cast<glm::mat4>(*target), static_cast<glm::vec3>(*translation));
}


void mathQuatFromAngle(Quaternion* result, float angle, Vector3* axis)
{
    *result = glm::angleAxis(angle, static_cast<glm::vec3>(*axis));
}

void mathRotateMatrix4(Matrix4* result, Matrix4* target, float angle, Vector3* axis)
{
    *result = glm::rotate(static_cast<glm::mat4>(*target), angle, static_cast<glm::vec3>(*axis));
}

void mathScaleMatrix4(Matrix4* result, Matrix4* target, Vector3* scale)
{
    *result = glm::scale(static_cast<glm::mat4>(*target), static_cast<glm::vec3>(*scale));
}

void mathMatrix4ToTransform(Transform* result, Matrix4* target)
{
    
    auto mat = static_cast<glm::mat4>(*target);

    const auto l = glm::vec3(mat[3]);

    result->Location = glm::vec3{l.x, l.y, l.z};
    
    result->Rotation = glm::quat(glm::mat3(mat));

    result->Scale = glm::vec3(length(glm::vec3(mat[0])), length(glm::vec3(mat[1])), length(glm::vec3(mat[2])));
}

void mathTransformToMatrix4(Matrix4* result, Transform* target)
{
    auto translated = glm::translate(glm::mat4(1.0f), static_cast<glm::vec3>(target->Location));
    auto rotated = translated * glm::mat4_cast(
            static_cast<glm::quat>(target->Rotation));
    auto scaled = glm::scale(rotated,static_cast<glm::vec3>(target->Scale));
    
    *result = scaled;
}

void mathGlmOrthographic(Matrix4* result, float left, float right, float bottom, float top)
{
    *result = glm::ortho(left,right,bottom,top);
}

void mathGlmPerspective(Matrix4* result, float fov, float aspect, float near, float far)
{
    *result = glm::perspective(fov,aspect,near,far);
}

void mathRotateMatrix3(Matrix3* result, Matrix3* target, float angle)
{
    *result = glm::rotate(static_cast<glm::mat3>(*target), angle);
}

void mathScaleMatrix3(Matrix3* result, Matrix3* target, Vector2* scale)
{
    *result = glm::scale(static_cast<glm::mat3>(*target), static_cast<glm::vec2>(*scale));
}

void mathTranslateMatrix3(Matrix3* result, Matrix3* target, Vector2* translation)
{
    *result = translate(static_cast<glm::mat3>(*target), static_cast<glm::vec2>(*translation));
}

void mathInverseMatrix3(Matrix3* result, Matrix3* a)
{
    *result = inverse(static_cast<glm::mat3>(*a));
}

void mathMultiplyMatrix3Matrix3(Matrix3* result, Matrix3* left, Matrix3* right)
{
    *result = static_cast<glm::mat3>(*left) * static_cast<glm::mat3>(*right);
}

void mathMultiplyMatrix3Vector3(Vector3* result, Matrix3* left, Vector3* right)
{
    *result = static_cast<glm::mat3>(*left) * static_cast<glm::vec3>(*right);
}
