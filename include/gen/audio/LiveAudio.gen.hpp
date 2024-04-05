#pragma once
#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"

#ifdef META_FILE_ID
#undef META_FILE_ID
#endif
#define META_FILE_ID mid52f3d55d989a4f0f9f6cf464b5f9f892


#define _meta_mid52f3d55d989a4f0f9f6cf464b5f9f892_14() \
static std::shared_ptr<meta::Metadata> Meta; \
std::shared_ptr<meta::Metadata> GetMeta() const;


