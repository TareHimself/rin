#pragma once
#include "reflect/Macro.hpp"
#include "reflect/Reflect.hpp"
#include "reflect/Factory.hpp"
#include "reflect/wrap/Wrap.hpp"


#ifndef _REFLECT_GENERATED_AllocatedBuffer
#define _REFLECT_GENERATED_AllocatedBuffer
#define _REFLECTED_GENERATED_AllocatedBuffer_PROPERTY_buffer REFLECT_WRAP_PROPERTY(AllocatedBuffer,buffer,vk::Buffer)

#define _REFLECT_GENERATE_AllocatedBuffer \
reflect::factory::ReflectTypeBuilder builder; \
builder.AddField(_REFLECTED_GENERATED_AllocatedBuffer_PROPERTY_buffer); \
builder.Create<AllocatedBuffer>("AllocatedBuffer");
#endif



#ifndef _REFLECT_GENERATED_AllocatedImage
#define _REFLECT_GENERATED_AllocatedImage
#define _REFLECTED_GENERATED_AllocatedImage_PROPERTY_image REFLECT_WRAP_PROPERTY(AllocatedImage,image,vk::Image)

#define _REFLECTED_GENERATED_AllocatedImage_PROPERTY_view REFLECT_WRAP_PROPERTY(AllocatedImage,view,vk::ImageView)

#define _REFLECTED_GENERATED_AllocatedImage_PROPERTY_extent REFLECT_WRAP_PROPERTY(AllocatedImage,extent,vk::Extent3D)

#define _REFLECTED_GENERATED_AllocatedImage_PROPERTY_format REFLECT_WRAP_PROPERTY(AllocatedImage,format,vk::Format)

#define _REFLECT_GENERATE_AllocatedImage \
reflect::factory::ReflectTypeBuilder builder; \
builder.AddField(_REFLECTED_GENERATED_AllocatedImage_PROPERTY_image); \
builder.AddField(_REFLECTED_GENERATED_AllocatedImage_PROPERTY_view); \
builder.AddField(_REFLECTED_GENERATED_AllocatedImage_PROPERTY_extent); \
builder.AddField(_REFLECTED_GENERATED_AllocatedImage_PROPERTY_format); \
builder.Create<AllocatedImage>("AllocatedImage");
#endif



