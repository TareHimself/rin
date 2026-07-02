#pragma once

#ifndef RIN_GLOBAL_SAMPLER_COUNT
#define RIN_GLOBAL_SAMPLER_COUNT 6
#endif

#ifndef RIN_GLOBAL_TEXTURE_COUNT
#define RIN_GLOBAL_TEXTURE_COUNT 2048
#endif

#ifndef RIN_GLOBAL_TEXTURE_ARRAY_COUNT
#define RIN_GLOBAL_TEXTURE_ARRAY_COUNT 512
#endif

#ifndef RIN_GLOBAL_CUBEMAP_COUNT
#define RIN_GLOBAL_CUBEMAP_COUNT 512
#endif

#ifndef RIN_FIRST_USER_SLOT
#define RIN_FIRST_USER_SLOT 1
#endif

#ifndef __RIN_INTERNAL_BINDING_FAKE
#define __RIN_INTERNAL_BINDING_FAKE(_set, _binding) vk::binding(_binding, _set)
#endif

#ifndef _RIN_INTERNAL_BINDING
#define _RIN_INTERNAL_BINDING(set, binding) __RIN_INTERNAL_BINDING_FAKE(set,binding)
#endif

#ifndef RIN_BINDING
#define RIN_BINDING(set, binding) _RIN_INTERNAL_BINDING(RIN_FIRST_USER_SLOT + set,binding)
#endif

