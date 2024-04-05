#pragma once
#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"

#ifdef META_FILE_ID
#undef META_FILE_ID
#endif
#define META_FILE_ID mid213ed90e4d2444a29134c937347639f6


#define _meta_mid213ed90e4d2444a29134c937347639f6_12() \
static std::shared_ptr<meta::Metadata> Meta; \
std::shared_ptr<meta::Metadata> GetMeta() const;


