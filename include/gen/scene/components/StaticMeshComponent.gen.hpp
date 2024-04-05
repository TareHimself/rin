#pragma once
#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"

#ifdef META_FILE_ID
#undef META_FILE_ID
#endif
#define META_FILE_ID midb6cdef64a99c4e7dac1cd6f1b0e1875f


#define _meta_midb6cdef64a99c4e7dac1cd6f1b0e1875f_12() \
static std::shared_ptr<meta::Metadata> Meta; \
std::shared_ptr<meta::Metadata> GetMeta() const;


