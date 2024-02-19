#pragma once
#include "reflect/Macro.hpp"
#include "reflect/Reflect.hpp"
#include "reflect/Factory.hpp"
#include "reflect/wrap/Wrap.hpp"


#ifndef _REFLECT_GENERATED_TestGameObject
#define _REFLECT_GENERATED_TestGameObject
#define _REFLECTED_GENERATED_TestGameObject_PROPERTY__mesh REFLECT_WRAP_PROPERTY(TestGameObject,_mesh,Managed<drawing::Mesh>)

#define _REFLECTED_GENERATED_TestGameObject_PROPERTY__meshComponent REFLECT_WRAP_PROPERTY(TestGameObject,_meshComponent,Ref<scene::StaticMeshComponent>)

#define _REFLECTED_GENERATED_TestGameObject_PROPERTY__scriptComp REFLECT_WRAP_PROPERTY(TestGameObject,_scriptComp,Ref<scene::ScriptComponent>)

#define _REFLECTED_GENERATED_TestGameObject_FUNCTION_GetWorldTransform REFLECT_WRAP_FUNCTION_BEGIN(GetWorldTransform) \
{ \
 \
if(result){ \
*result.As<math::Transform>() = instance.As<TestGameObject>()->GetWorldTransform(); \
} \
})

#define _REFLECTED_GENERATED_TestGameObject_FUNCTION_Construct REFLECT_WRAP_FUNCTION_BEGIN(Construct) \
{ \
 \
if(result){ \
*result.As< Managed<TestGameObject>>() = TestGameObject::Construct(); \
} \
})

#define _REFLECT_GENERATE_TestGameObject \
reflect::factory::ReflectTypeBuilder builder; \
builder.AddField(_REFLECTED_GENERATED_TestGameObject_PROPERTY__mesh); \
builder.AddField(_REFLECTED_GENERATED_TestGameObject_PROPERTY__meshComponent); \
builder.AddField(_REFLECTED_GENERATED_TestGameObject_PROPERTY__scriptComp); \
builder.AddField(_REFLECTED_GENERATED_TestGameObject_FUNCTION_GetWorldTransform); \
builder.AddField(_REFLECTED_GENERATED_TestGameObject_FUNCTION_Construct); \
builder.Create<TestGameObject>("TestGameObject");
#endif


