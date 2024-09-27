#pragma once

#define MCLASS(...)
#define MPROPERTY(...)
#define MFUNCTION(...)


#define META_CONCAT_IMPL(x, y) x##y

#define META_CONCAT(x, y) META_CONCAT_IMPL(x, y)

#define META_JOIN_IMPL(A,B) A##B

#define META_JOIN(A,B) META_JOIN_IMPL(A,B)

#define MBODY() META_JOIN(_meta_,META_JOIN(META_FILE_ID,META_JOIN(_,__LINE__)))

#ifdef _MSC_VER
#define META_TYPE_NAME __FUNCSIG__
#else
#define META_TYPE_NAME __PRETTY_FUNCTION__
#endif

