#pragma once
#include <memory>
#include "Metadata.hpp"
#include "aerox/json.hpp"
#include "gen/meta/IMetadata.gen.hpp"

namespace aerox::meta
{
    META_TYPE()
    class IMetadata
    {
        META_BODY()

        META_FUNCTION()
        virtual json ToJson();

        META_FUNCTION()
        virtual void FromJson(json & data);
    };
}
