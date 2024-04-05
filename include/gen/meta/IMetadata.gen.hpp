#pragma once
#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"

#ifdef META_FILE_ID
#undef META_FILE_ID
#endif
#define META_FILE_ID mid38b5aa2434984d619890f19e26f7a68e


#define _meta_mid38b5aa2434984d619890f19e26f7a68e_12() \
static std::shared_ptr<meta::Metadata> Meta; \
std::shared_ptr<meta::Metadata> GetMeta() const;


