#pragma once

#include "aerox/containers/Serializable.hpp"
#include "aerox/containers/String.hpp"
#include "aerox/drawing/types.hpp"
#include <cstdint>

namespace aerox::assets {

    namespace types {
        const String MESH = "MESH";
        const String TEXTURE = "TEXTURE";
        const String FONT = "FONT";
    }

    struct VEngineAssetHeader : Serializable {

        void ReadFrom(Buffer &store) override;

        void WriteTo(Buffer &store) override;
    };
}