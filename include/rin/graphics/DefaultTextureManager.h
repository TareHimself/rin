#pragma once
#include "ITextureManager.h"

namespace rin::graphics
{
    class DefaultTextureManager : public ITextureManager
    {
    protected:
        void OnDispose() override;

    public:
    
    };
}
