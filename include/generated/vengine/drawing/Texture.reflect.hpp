#pragma once
#include "reflect/Macro.hpp"
#include "reflect/Reflect.hpp"
#include "reflect/Factory.hpp"
#include "reflect/wrap/Wrap.hpp"


#ifndef _REFLECT_GENERATED_Texture
#define _REFLECT_GENERATED_Texture
#define _REFLECT_GENERATE_Texture \
reflect::factory::ReflectTypeBuilder builder; \
builder.Create<Texture>("Texture");
#endif



