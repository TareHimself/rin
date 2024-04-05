#pragma once
#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"

#ifdef META_FILE_ID
#undef META_FILE_ID
#endif
#define META_FILE_ID mid66426991af9a4c20a3f08053b344b186


#define _meta_mid66426991af9a4c20a3f08053b344b186_12() \
static std::shared_ptr<meta::Metadata> Meta; \
std::shared_ptr<meta::Metadata> GetMeta() const;


